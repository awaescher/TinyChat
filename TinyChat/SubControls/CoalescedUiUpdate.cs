using System.Runtime.ExceptionServices;

namespace TinyChat;

/// <summary>
/// Coalesces multiple update requests into a single callback on the owning control's UI thread.
/// </summary>
internal sealed class CoalescedUiUpdate
{
	private readonly Control _owner;
	private readonly int _ownerThreadId;
	private readonly Action _update;
	private int _cancelled;
	private int _pending;
	private ExceptionDispatchInfo? _updateException;

	public CoalescedUiUpdate(Control owner, Action update)
	{
		_owner = owner;
		_ownerThreadId = Environment.CurrentManagedThreadId;
		_update = update;
		_owner.HandleCreated += Owner_HandleCreated;
	}

	public void Queue()
	{
		if (Volatile.Read(ref _cancelled) != 0 || _owner.IsDisposed || _owner.Disposing)
			return;

		if (!_owner.IsHandleCreated)
		{
			if (Environment.CurrentManagedThreadId == _ownerThreadId)
			{
				Interlocked.Exchange(ref _pending, 0);
				_update();
			}
			else
			{
				Interlocked.Exchange(ref _pending, 1);
			}
			return;
		}

		if (Interlocked.Exchange(ref _pending, 1) != 0)
			return;

		try
		{
			_owner.BeginInvoke(ProcessPendingUpdate);
		}
		catch (InvalidOperationException) when (_owner.IsDisposed || _owner.Disposing || !_owner.IsHandleCreated)
		{
			Interlocked.Exchange(ref _pending, 0);
		}
	}

	public void Flush()
	{
		if (Volatile.Read(ref _cancelled) != 0 || _owner.IsDisposed || _owner.Disposing)
		{
			Interlocked.Exchange(ref _pending, 0);
			return;
		}

		if (Environment.CurrentManagedThreadId != _ownerThreadId || _owner.InvokeRequired)
		{
			if (!_owner.IsHandleCreated)
			{
				Queue();
				return;
			}

			try
			{
				_owner.Invoke((Action)Flush);
			}
			catch (InvalidOperationException) when (_owner.IsDisposed || _owner.Disposing || !_owner.IsHandleCreated)
			{
				Queue();
			}
			return;
		}

		if (Interlocked.Exchange(ref _pending, 0) != 0)
			CaptureUpdateException();

		Interlocked.Exchange(ref _updateException, null)?.Throw();
	}

	public void Cancel()
	{
		if (Interlocked.Exchange(ref _cancelled, 1) != 0)
			return;

		_owner.HandleCreated -= Owner_HandleCreated;
		Interlocked.Exchange(ref _pending, 0);
		Interlocked.Exchange(ref _updateException, null);
	}

	private void ProcessPendingUpdate()
	{
		if (Interlocked.Exchange(ref _pending, 0) == 0 ||
			Volatile.Read(ref _cancelled) != 0 ||
			_owner.IsDisposed ||
			_owner.Disposing)
		{
			return;
		}

		CaptureUpdateException();
	}

	private void CaptureUpdateException()
	{
		try
		{
			_update();
		}
		catch (Exception ex)
		{
			Interlocked.CompareExchange(ref _updateException, ExceptionDispatchInfo.Capture(ex), null);
		}
	}

	private void Owner_HandleCreated(object? sender, EventArgs e)
	{
		if (Interlocked.Exchange(ref _pending, 0) == 0 || Volatile.Read(ref _cancelled) != 0)
			return;

		_update();
	}
}

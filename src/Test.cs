/*
 *
 * Copyright (c) 2024 Carbon Community
 * All rights reserved.
 *
 */

using System;
using System.Reflection;
using API.Logger;

namespace Carbon.Test;

[AttributeUsage(AttributeTargets.Method)]
public class Test : Attribute
{
	public string Name;
	public float Timeout = -1;
	public bool CancelOnFail = false;

	public enum StatusTypes
	{
		None,
		Running,
		Success,
		Failure,
		Fatal,
		Timeout
	}

	internal Type _type;
	internal MethodInfo _method;
	internal object _target;
	internal StatusTypes _statusType;

	internal TimeSpan _duration;

	internal static object[] _args = new object[1];

	public bool IsRunning => Status == StatusTypes.Running;
	public StatusTypes Status => _statusType;

	public void Setup(object target, Type type, MethodInfo info)
	{
		_type = type;
		_method = info;
		_target = target;
	}
	public void SetDuration(TimeSpan span)
	{
		_duration = span;
	}
	public void SetStatus(StatusTypes status)
	{
		_statusType = status;
	}

	public void Run()
	{
		SetStatus(StatusTypes.Running);

		_args[0] = this;
		_method.Invoke(_target, _args);
	}
	public void RunCheck()
	{
		if (Timeout <= 0)
		{
			return;
		}

		if (_duration.TotalSeconds >= Timeout)
		{
			OnTimeout();
		}
	}

	public void OnSucceed()
	{
		if (!IsRunning)
		{
			return;
		}

		SetStatus(StatusTypes.Success);
		Log($"Success");
	}
	public void OnTimeout()
	{
		if (!IsRunning)
		{
			return;
		}

		SetStatus(StatusTypes.Timeout);
		Warn($"Timed out >= {Timeout * 1000:00}ms");
	}
	public void Fail(string reason, Exception ex = null)
	{
		if (!IsRunning)
		{
			return;
		}

		SetStatus(StatusTypes.Failure);
		Error($"Fail - {reason}", ex);
	}
	public void FatalFail(string reason, Exception ex = null)
	{
		if (!IsRunning)
		{
			return;
		}

		SetStatus(StatusTypes.Fatal);
		Error($"Fatal - {reason}", ex);
	}

	public bool ShouldCancel() => Status == StatusTypes.Fatal || (CancelOnFail && Status != StatusTypes.Success);

	public virtual string ToPrettyString() => $"{Name}|{_method.Name}|{_duration.TotalMilliseconds:00}ms";

	public void Log(string message)
	{
		Integrations.Logger.Console($"{ToPrettyString()}  {message}");
	}
	public void Warn(string message)
	{
		Integrations.Logger.Console($"{ToPrettyString()}  {message}", Severity.Warning);
	}
	public void Error(string message, Exception exception)
	{
		Integrations.Logger.Console($"{ToPrettyString()}  {message}", Severity.Error, exception);
	}
	public void Fatal(string message, Exception exception)
	{
		Error(message, exception);
		SetStatus(StatusTypes.Fatal);
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class Assert : Test
	{
		public override string ToPrettyString() => base.ToPrettyString() + "|Assert";

		public bool IsTrue(bool condition)
		{
			if (condition)
			{
				return true;
			}

			Fail($"IsTrue failed - [bool condition] {condition}");
			return false;
		}
		public bool IsFalse(bool condition)
		{
			if (!condition)
			{
				return true;
			}

			Fail($"IsFalse failed - [bool condition] {condition}");
			return false;
		}
		public bool IsNull(object value)
		{
			if (value == null)
			{
				return true;
			}

			Fail($"IsNull failed - [object value] {value}");
			return false;
		}
		public bool IsNotNull(object value)
		{
			if (value != null)
			{
				return true;
			}

			Fail($"IsNotNull failed - [object value] {value}");
			return false;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class WaitUntil : Test
	{
		public override string ToPrettyString() => base.ToPrettyString() + "|WaitUntil";


	}
}

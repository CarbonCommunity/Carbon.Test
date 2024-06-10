/*
 *
 * Copyright (c) 2024 Carbon Community
 * All rights reserved.
 *
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using API.Logger;
using UnityEngine;
using ILogger = API.Logger.ILogger;

namespace Carbon.Test;

public static partial class Integrations
{
	[AttributeUsage(AttributeTargets.Method)]
	public class Test : Attribute
	{
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

		public virtual string ToPrettyString() => $"{_type.Name}.{_method.Name}|{_duration.TotalMilliseconds:0}ms".ToLower();

		public void Log(object message)
		{
			Logger.Console($"{ToPrettyString()}  {(message == null ? "no message" : message.ToString())}");
		}

		public void Warn(object message)
		{
			Logger.Console($"{ToPrettyString()}  {(message == null ? "no message" : message.ToString())}", Severity.Warning);
		}

		public void Error(object message, Exception exception)
		{
			Logger.Console($"{ToPrettyString()}  {(message == null ? "no message" : message.ToString())}", Severity.Error, exception);
		}

		public void Fatal(object message, Exception exception)
		{
			Error(message, exception);
			SetStatus(StatusTypes.Fatal);
		}

		[AttributeUsage(AttributeTargets.Method)]
		public class Assert : Test
		{
			public override string ToPrettyString() => base.ToPrettyString() + "|assert";

			public bool IsTrue(bool condition)
			{
				if (condition)
				{
					Warn($"IsTrue success - [bool condition] {condition}");
;					return true;
				}

				Fail($"IsTrue failed - [bool condition] {condition}");
				return false;
			}

			public bool IsFalse(bool condition)
			{
				if (!condition)
				{
					Warn($"IsFalse success - [bool condition] {condition}");
					return true;
				}

				Fail($"IsFalse failed - [bool condition] {condition}");
				return false;
			}

			public bool IsNull(object value)
			{
				if (value == null)
				{
					Warn($"IsNull success - [object value] {value}");
					return true;
				}

				Fail($"IsNull failed - [object value] {value}");
				return false;
			}

			public bool IsNotNull(object value)
			{
				if (value != null)
				{
					Warn($"IsNotNull success - [object value] {value}");
					return true;
				}

				Fail($"IsNotNull failed - [object value] {value}");
				return false;
			}
		}
	}
}

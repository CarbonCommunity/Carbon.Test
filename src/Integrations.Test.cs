/*
 *
 * Copyright (c) 2024 Carbon Community
 * All rights reserved.
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using API.Logger;

namespace Carbon.Test;

public static partial class Integrations
{
	[AttributeUsage(AttributeTargets.Method)]
	public class Test : Attribute
	{
		public float Timeout = 1000f;
		public bool CancelOnFail = true;

		public enum StatusTypes
		{
			None,
			Running,
			Complete,
			Failed,
			Fatal,
			Timeout
		}

		internal List<Exception> _exceptions = new();
		internal Type _type;
		internal MethodInfo _method;
		internal object _target;
		internal StatusTypes _statusType;
		internal static int _prefixScale;

		internal TimeSpan _duration;

		internal static object[] _args = new object[1];

		public bool IsRunning => Status == StatusTypes.Running;
		public StatusTypes Status => _statusType;
		public IEnumerable<Exception> Exceptions => _exceptions.AsEnumerable();

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

			try
			{
				_method.Invoke(_target, _args);
			}
			catch (Exception exception)
			{
				_exceptions.Add(exception);
			}
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

		public void Complete()
		{
			if (!IsRunning)
			{
				return;
			}

			SetStatus(StatusTypes.Complete);
			Log($"Complete - {_exceptions.Count} excp.");
		}

		internal void OnTimeout()
		{
			if (!IsRunning)
			{
				return;
			}

			SetStatus(StatusTypes.Timeout);
			Warn($"Timed out >= {Timeout * 1000:00}ms");
		}

		public void Fail(string message, Exception ex = null)
		{
			if (!IsRunning)
			{
				return;
			}

			SetStatus(StatusTypes.Failed);
			Error($"Fail - {message}", ex);
		}

		public void FatalFail(string message, Exception ex = null)
		{
			if (!IsRunning)
			{
				return;
			}

			SetStatus(StatusTypes.Fatal);
			Error($"Fatal - {message}", ex);
		}

		public bool ShouldCancel() => Status == StatusTypes.Fatal || (CancelOnFail && Status != StatusTypes.Complete);

		public virtual string ToPrettyString() => $"{_type.Name}.{_method.Name}|{_duration.TotalMilliseconds:0}ms|".ToLower();

		public void CalculatePrettyString(out string mainString, out string spacing)
		{
			mainString = ToPrettyString();

			var currentStringLength = mainString.Length;

			if (currentStringLength > _prefixScale)
			{
				_prefixScale = currentStringLength;
			}

			spacing = new string(' ', _prefixScale - currentStringLength);
		}

		public void Log(object message)
		{
			CalculatePrettyString(out var main, out var second);
			Logger.Console($"{second}{main}  {(message == null ? "no message" : message.ToString())}");
		}

		public void Warn(object message)
		{
			CalculatePrettyString(out var main, out var second);
			Logger.Console($"{second}{main}  {(message == null ? "no message" : message.ToString())}", Severity.Warning);
		}

		public void Error(object message, Exception exception)
		{
			CalculatePrettyString(out var main, out var second);
			Logger.Console($"{second}{main}  {(message == null ? "no message" : message.ToString())}", Severity.Error, exception);
		}

		public void Fatal(object message, Exception exception)
		{
			Error(message, exception);
			SetStatus(StatusTypes.Fatal);
		}

		[AttributeUsage(AttributeTargets.Method)]
		public class Assert : Test
		{
			public override string ToPrettyString() => base.ToPrettyString() + "assert|";

			public bool IsTrue(bool condition)
			{
				if (condition)
				{
					Warn($"IsTrue passed    - [bool condition] {condition}");
;					return true;
				}

				Fail($"IsTrue failed    - [bool condition] {condition}");
				return false;
			}

			public bool IsFalse(bool condition)
			{
				if (!condition)
				{
					Warn($"IsFalse passed   - [bool condition] {condition}");
					return true;
				}

				Fail($"IsFalse failed   - [bool condition] {condition}");
				return false;
			}

			public bool IsNull(object value)
			{
				if (value == null)
				{
					Warn($"IsNull passed    - [object value] {value}");
					return true;
				}

				Fail($"IsNull failed    - [object value] {value}");
				return false;
			}

			public bool IsNotNull(object value)
			{
				if (value != null)
				{
					Warn($"IsNotNull passed - [object value] {value}");
					return true;
				}

				Fail($"IsNotNull failed - [object value] {value}");
				return false;
			}
		}
	}
}

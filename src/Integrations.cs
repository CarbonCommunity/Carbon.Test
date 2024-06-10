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
	public static ILogger Logger { get; set; }
	public static Stopwatch Stopwatch { get; } = new();
	public static Queue<TestBed> Queue { get; } = new();

	public static TestBed Get(string context, Type type, object target)
	{
		var bed = new TestBed(context);

		foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			var attribute = method.GetCustomAttribute<Test>();

			if (attribute == null)
			{
				continue;
			}

			bed.AddTest(target, type, method, attribute);
		}

		return bed;
	}

	public static void QueueBed(TestBed bed)
	{
		Queue.Enqueue(bed);
	}

	public static IEnumerator RunQueue(float delay)
	{
		while (Queue.Count != 0)
		{
			var bed = Queue.Dequeue();

			Logger.Console($"Initialized Test bed - {bed.Context}");
			foreach (var test in bed)
			{
				Stopwatch.Restart();

				test.Run();

				while (test.IsRunning)
				{
					test.SetDuration(Stopwatch.Elapsed);
					test.RunCheck();
					yield return null;
				}

				if (test.ShouldCancel())
				{
					Logger.Console($"Cancelled due to fatal status - {bed.Context}", Severity.Error);
					break;
				}

				if (delay > 0)
				{
					yield return CoroutineEx.waitForSecondsRealtime(delay);
				}
				else
				{
					yield return null;
				}
			}

			Logger.Console($"Completed {bed.Count:n0} tests - {bed.Context}");

			yield return null;
		}
	}

	public static void ClearQueue()
	{
		Queue.Clear();
	}

	public class TestBed : List<Test>
	{
		public string Context;

		public TestBed() { }

		public TestBed(string context)
		{
			Context = context;
		}

		public void AddTest(object target, Type type, MethodInfo method, Test test)
		{
			test.Setup(target, type, method);
			Add(test);
		}
	}
}

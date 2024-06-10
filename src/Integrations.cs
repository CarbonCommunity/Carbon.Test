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
using UnityEngine;
using ILogger = API.Logger.ILogger;

namespace Carbon.Test;

public static class Integrations
{
	public static ILogger Logger { get; set; }
	public static Stopwatch Stopwatch { get; } = new();
	public static Queue<TestBed> TestBeds { get; } = new();

	public static TestBed GetTestBed(string context, Type type)
	{
		var bed = new TestBed(context);

		foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
		{
			var attribute = method.GetCustomAttribute<Test>();

			if (attribute == null)
			{
				continue;
			}

			bed.AddTest(type, method, attribute);
		}

		return bed;
	}
	public static IEnumerator Run(float delay)
	{
		while (TestBeds.Count != 0)
		{
			var bed = TestBeds.Dequeue();

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

	public class TestBed : List<Test>
	{
		public string Context;

		public TestBed() { }
		public TestBed(string context)
		{
			Context = context;
		}

		public void AddTest(Type type, MethodInfo method, Test test)
		{
			test.Setup(type, method);
			Add(test);
		}
	}
}

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
	public static Queue<TestBank> Banks { get; } = new();

	public static TestBank Get(string context, Type type, object target)
	{
		var bed = new TestBank(context);

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

	public static void EnqueueBed(TestBank bank)
	{
		Banks.Enqueue(bank);
	}

	public static void Run(float delay)
	{
		ServerMgr.Instance.StartCoroutine(RunRoutine(delay));
	}

	public static IEnumerator RunRoutine(float delay)
	{
		while (Banks.Count != 0)
		{
			var bank = Banks.Dequeue();
			var completed = 0;

			Logger.Console($"initialized testbed - context: {bank.Context}");

			foreach (var test in bank)
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
					Logger.Console($"cancelled due to fatal status - context: {bank.Context}", Severity.Error);
					break;
				}

				completed++;

				if (delay > 0)
				{
					yield return CoroutineEx.waitForSecondsRealtime(delay);
				}
				else
				{
					yield return null;
				}
			}

			Logger.Console($"completed {completed:n0} out of {bank.Count:n0} {(bank.Count == 1 ? "test" : "tests")} - context: {bank.Context}");

			yield return null;
		}
	}

	public static void Clear()
	{
		Banks.Clear();
	}

	public class TestBank : List<Test>
	{
		public string Context;

		public TestBank(string context)
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

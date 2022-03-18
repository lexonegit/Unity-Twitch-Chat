using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script is used to send multithreaded operations to the main Unity thread
/// </summary>
public class MainThread : MonoBehaviour
{
    public static MainThread Instance { get; set; }

    private static readonly Queue<Action> taskQueue = new Queue<Action>();

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Execute tasks
    /// </summary>
    private void Update()
    {
        lock (taskQueue)
        {
            while (taskQueue.Count > 0)
            {
                taskQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Enqueue a new task for the main thread
    /// </summary>
    public void Enqueue(Action a)
    {
        lock (taskQueue)
        {
            taskQueue.Enqueue(a);
        }
    }

    public void Clear() 
    {
        taskQueue.Clear();
    }
}

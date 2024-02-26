using System;
using UnityEngine;
using UnityEngine.Analytics;

namespace Assets.EDO
{
    /// <summary>
    /// Interface for Event Driven Object (EDO) that will listen for messages from ISenders
    /// </summary>
    public interface IReceiver
    {
        public void EventReceived();
    }
}
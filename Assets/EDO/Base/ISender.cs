namespace Assets.EDO
{
    /// <summary>
    /// Interface for Event Driven Object (EDO) that will send messages to IReceivers
    /// </summary>
    public interface ISender
    {
        public delegate void EventConditionsMetHandler();

        public event EventConditionsMetHandler OnEventConditionsMet;

        public void Subscribe(EventConditionsMetHandler actionToPerform) => OnEventConditionsMet += actionToPerform;
        public void Unsubscribe(EventConditionsMetHandler actionToUnsubscribe) => OnEventConditionsMet -= actionToUnsubscribe;

        public void FireEvent();
    }
}
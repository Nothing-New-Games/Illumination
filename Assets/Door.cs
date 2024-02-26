using Assets.EDO;
using UnityEngine;

public class Door : MonoBehaviour, IReceiver
{
    public GameObject Sender;

    private void Start()
    {
        if (Sender == null || Sender.GetComponent<ISender>() == null)
        {
            Debug.LogError("No senders found for the event listener!");
            enabled = false;
            return;
        }


        Sender.GetComponent<ISender>().Subscribe(EventReceived);
        Debug.Log("Subscribed to event!");
    }


    public void EventReceived()
    {
        Debug.Log("We have lift off!");
    }
}

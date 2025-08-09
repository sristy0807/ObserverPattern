#Core contracts (generic, reusable)


public interface IEventObserver<TPayload>
{
    void OnEventRaised(TPayload payload);
}

public interface IEventSubject<TPayload>
{
    void AddObserver(IEventObserver<TPayload> observer);
    void RemoveObserver(IEventObserver<TPayload> observer);
    void NotifyObservers(TPayload payload);
}

public class EventSubject<TPayload> : IEventSubject<TPayload>
{
    private readonly System.Collections.Generic.List<IEventObserver<TPayload>> _observers = new();

    public void AddObserver(IEventObserver<TPayload> observer)
    {
        if (observer != null && !_observers.Contains(observer)) _observers.Add(observer);
    }

    public void RemoveObserver(IEventObserver<TPayload> observer)
    {
        _observers.Remove(observer);
    }

    public void NotifyObservers(TPayload payload)
    {
        // create snapshot to avoid modification during iteration
        var snapshot = _observers.ToArray();
        for (int i = 0; i < snapshot.Length; i++) snapshot[i].OnEventRaised(payload);
    }
}



#Define events as types (ISP-friendly)

// Payloads
public class RegisterPatientModel { public string FirstName, LastName, MRN; /* ... */ }
public class UpdatePatientModel   { public string MRN; public string NewLastName; /* ... */ }

// Event-specific observer interfaces (small and precise)
public interface IPatientRegisteredObserver : IEventObserver<RegisterPatientModel> {}
public interface IPatientUpdatedObserver    : IEventObserver<UpdatePatientModel>    {}

// Subjects per event (can be fields on a MonoBehaviour “EventHub”, or ScriptableObjects)
public class PatientRegisteredSubject : EventSubject<RegisterPatientModel> {}
public class PatientUpdatedSubject    : EventSubject<UpdatePatientModel>    {}


#Wiring in Unity
using UnityEngine;

public class EventHub : MonoBehaviour
{
    // Scene-owned subjects, assign by reference (or make these ScriptableObjects)
    [SerializeField] private PatientRegisteredSubject patientRegistered = new();
    [SerializeField] private PatientUpdatedSubject    patientUpdated    = new();

    public IEventSubject<RegisterPatientModel> PatientRegistered => patientRegistered;
    public IEventSubject<UpdatePatientModel>   PatientUpdated    => patientUpdated;
}

using UnityEngine;

public class RegisterPatientView : MonoBehaviour
{
    [SerializeField] private EventHub hub;

    public void OnClickRegisterPatient()
    {
        var model = new RegisterPatientModel { FirstName="John", LastName="Doe", MRN="MRN001" };
        hub.PatientRegistered.NotifyObservers(model);
    }
}

using UnityEngine;

// A controller that listens to one or multiple events by implementing small interfaces
public class PatientController : MonoBehaviour, IPatientRegisteredObserver, IPatientUpdatedObserver
{
    [SerializeField] private EventHub hub;

    private void OnEnable()
    {
        hub.PatientRegistered.AddObserver(this);
        hub.PatientUpdated.AddObserver(this);
    }

    private void OnDisable()
    {
        hub.PatientRegistered.RemoveObserver(this);
        hub.PatientUpdated.RemoveObserver(this);
    }

    // Event handlers
    public void OnEventRaised(RegisterPatientModel m)
    {
        Debug.Log($"Registered: {m.FirstName} {m.LastName} ({m.MRN})");
        // business logic...
    }

    public void OnEventRaised(UpdatePatientModel m)
    {
        Debug.Log($"Updated: {m.MRN} -> {m.NewLastName}");
        // business logic...
    }
}

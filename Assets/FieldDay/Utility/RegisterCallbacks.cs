namespace FieldDay {
    public interface IRegistrationCallbacks {
        void OnRegister();
        void OnDeregister();
    }

    static public class RegistrationCallbacks
    {
        static public void InvokeRegister(object obj) {
            (obj as IRegistrationCallbacks)?.OnRegister();
        }

        static public void InvokeDeregister(object obj) {
            (obj as IRegistrationCallbacks)?.OnDeregister();
        }
    }
}
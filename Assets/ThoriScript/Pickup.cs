using UnityEngine;

public class Pickup : MonoBehaviour
{
    public int throwableIndex;
    public int amount = 1;

    private void OnTriggerEnter(Collider other)
    {
        ObjectLogic inventory = other.GetComponent<ObjectLogic>();

        if (inventory != null)
        {
            inventory.AddThrowable(throwableIndex, amount);
            Destroy(gameObject);
        }
    }
}

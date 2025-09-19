using UnityEngine;

public class ObjectPanelHandler : MonoBehaviour
{
    [SerializeField] private GameObject objectMenu;
    [SerializeField] private InteractionController interactions;
    private void ShowObjectMenu() 
    {
        objectMenu.SetActive(true);
    }
    private void Move()
    {
        //interactions.IsMoving = true;
    }
}

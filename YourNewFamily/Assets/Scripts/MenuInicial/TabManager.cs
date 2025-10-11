using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject[] tabs;

    void Start()
    {
        // Activar solo el primer tab al inicio
        if (tabs.Length > 0)
        {
            ShowTab(0);
        }
    }

    public void ShowTab(int tabIndex)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].SetActive(i == tabIndex);
        }
    }
}

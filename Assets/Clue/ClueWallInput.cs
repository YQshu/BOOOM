using UnityEngine;

public class ClueWallInput : MonoBehaviour
{
    [SerializeField] private KeyCode openKey = KeyCode.Tab;
    [SerializeField] private ClueMana clueMana;

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            if (clueMana != null)
                clueMana.ToggleClueWall();
        }
    }
}

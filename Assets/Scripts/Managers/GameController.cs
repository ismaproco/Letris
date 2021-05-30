using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Start is called before the first frame update
    Board m_gameBoard;
    Spawner m_spawner;

    void Start()
    {
        m_gameBoard = GameObject.FindWithTag("Board").GetComponent<Board>();
        m_spawner = GameObject.FindWithTag("Spawner").GetComponent<Spawner>();

        if (!m_gameBoard || !m_spawner)
        {
            Debug.Log(
                "ERROR board or spawner not present"
            );
        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}

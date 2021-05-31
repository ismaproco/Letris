using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Start is called before the first frame update
    Board m_gameBoard;
    Spawner m_spawner;

    Shape m_activeShape;

    float m_dropInterval = 0.2f;
    float m_timeToDrop;

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

        if (m_spawner)
        {
            if (m_activeShape == null)
            {
                m_activeShape = m_spawner.SpawnShape();
            }
            m_spawner.transform.position = Vectorf.Round(m_spawner.transform.position);
        }

    }

    // Update is called once per frame
    void Update()
    {
        // no spawner or no gameboard no game
        if (!m_gameBoard || !m_spawner)
        {
            return;
        }

        if (Time.time > m_timeToDrop)
        {
            m_timeToDrop = Time.time + m_dropInterval;
            if (m_activeShape)
            {
                m_activeShape.MoveDown();
                if (!m_gameBoard.IsValidPosition(m_activeShape))
                {
                    m_activeShape.MoveUp();
                    m_gameBoard.StoreShapeInGrid(m_activeShape);
                    if (m_spawner)
                    {
                        m_activeShape = m_spawner.SpawnShape();
                    }
                }
            }
        }
    }
}

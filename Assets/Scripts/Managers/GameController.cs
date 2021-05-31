using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Start is called before the first frame update
    Board m_gameBoard;
    Spawner m_spawner;

    Shape m_activeShape;

    public float m_dropInterval = 1f;
    float m_timeToDrop;


    float m_timeToNextKeyLeftRight;

    [Range(0.02f, 1f)]
    public float m_keyRepeatRateLeftRight = 0.1f;

    [Range(0.01f, 1f)]
    public float m_keyRepeatRateDown = 0.01f;
    float m_timeToNextKeyDown;


    [Range(0.02f, 1f)]
    public float m_keyRepeatRateRotate = 0.12f;
    float m_timeToNextKeyRotate;

    bool m_gameOver = false;

    public GameObject m_gameOverPanel;
    SoundManager m_soundManager;

    void Start()
    {
        //m_timeToNextKey = Time.time;

        m_timeToNextKeyLeftRight = Time.time + m_keyRepeatRateLeftRight;
        m_timeToNextKeyRotate = Time.time + m_keyRepeatRateRotate;
        m_timeToNextKeyDown = Time.time + m_keyRepeatRateDown;

        m_gameBoard = GameObject.FindWithTag("Board").GetComponent<Board>();
        m_spawner = GameObject.FindWithTag("Spawner").GetComponent<Spawner>();
        m_soundManager = GameObject.FindObjectOfType<SoundManager>();


        if (!m_gameBoard || !m_spawner || !m_soundManager)
        {
            Debug.Log(
                "ERROR board ,spawner, or soundManager not present"
            );
        }

        if (m_spawner)
        {
            m_spawner.transform.position = Vectorf.Round(m_spawner.transform.position);
            if (!m_activeShape)
            {
                m_activeShape = m_spawner.SpawnShape();
            }
        }

        if (m_gameOverPanel)
        {
            m_gameOverPanel.SetActive(false);
        }

    }

    void playSound(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (m_soundManager.m_fxEnabled && clip)
        {
            AudioSource.PlayClipAtPoint(
                clip,
                Camera.main.transform.position,
                Mathf.Clamp(m_soundManager.m_fxVolume * volumeMultiplier, 0.05f, 1f));
        }
    }


    void LandShape()
    {
        m_activeShape.MoveUp();
        m_gameBoard.StoreShapeInGrid(m_activeShape);
        m_activeShape = m_spawner.SpawnShape();

        m_timeToNextKeyLeftRight = Time.time;
        m_timeToNextKeyRotate = Time.time;
        m_timeToNextKeyDown = Time.time;

        m_gameBoard.ClearAllRows();
        playSound(m_soundManager.m_dropSound);
    }

    void PlayerInput()
    {
        if (Input.GetButton("MoveRight") && Time.time > m_timeToNextKeyLeftRight
                    || Input.GetButtonDown("MoveRight"))
        {
            m_activeShape.MoveRight();
            m_timeToNextKeyLeftRight = Time.time + m_keyRepeatRateLeftRight;

            if (!m_gameBoard.IsValidPosition(m_activeShape))
            {
                m_activeShape.MoveLeft();
                playSound(m_soundManager.m_errorSound);
            }
            else
            {
                playSound(m_soundManager.m_moveSound, 0.5f);
            }
        }
        else if (Input.GetButton("MoveLeft") && Time.time > m_timeToNextKeyLeftRight
                  || Input.GetButtonDown("MoveLeft"))
        {
            m_activeShape.MoveLeft();
            m_timeToNextKeyLeftRight = Time.time + m_keyRepeatRateLeftRight;

            if (!m_gameBoard.IsValidPosition(m_activeShape))
            {
                m_activeShape.MoveRight();
                playSound(m_soundManager.m_errorSound);
            }
            else
            {
                playSound(m_soundManager.m_moveSound, 0.5f);
            }
        }
        else if (Input.GetButtonDown("Rotate") && Time.time > m_timeToNextKeyRotate)
        {
            m_activeShape.RotateRight();
            m_timeToNextKeyRotate = Time.time + m_keyRepeatRateRotate;

            if (!m_gameBoard.IsValidPosition(m_activeShape))
            {
                m_activeShape.RotateLeft();
                playSound(m_soundManager.m_errorSound);
            }
            else
            {
                playSound(m_soundManager.m_moveSound, 0.5f);
            }
        }
        else if (Input.GetButton("MoveDown")
            && Time.time > m_timeToNextKeyDown
            || Time.time > m_timeToDrop)
        {
            m_timeToDrop = Time.time + m_dropInterval;
            m_timeToNextKeyDown = Time.time + m_keyRepeatRateDown;
            m_activeShape.MoveDown();

            if (!m_gameBoard.IsValidPosition(m_activeShape))
            {
                if (m_gameBoard.IsOverLimit(m_activeShape))
                {
                    GameOver();
                }
                else
                {
                    LandShape();
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // no spawner or no gameboard no game
        if (!m_gameBoard || !m_spawner || !m_activeShape || m_gameOver || !m_soundManager)
        {
            return;
        }

        PlayerInput();
    }

    public void Restart()
    {
        Application.LoadLevel(Application.loadedLevel);
    }

    void GameOver()
    {
        m_activeShape.MoveUp();
        m_gameOver = true;
        if (m_gameOverPanel)
        {
            m_gameOverPanel.SetActive(true);
        }
        playSound(m_soundManager.m_gameOverSound);
    }
}

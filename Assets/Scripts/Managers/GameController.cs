using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    // Start is called before the first frame update
    Board m_gameBoard;
    Spawner m_spawner;

    ScoreManager m_scoreManager;

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

    public IconToggle m_rotateIconToggle;
    bool m_clockwise = true;
    public bool m_isPaused = false;
    public GameObject m_pausePanel;

    float m_dropIntervalModded;

    Ghost m_ghost;
    Holder m_holder;

    public Text m_diagnosticText;

    enum Direction { none, left, right, up, down }
    Direction m_dragDirection = Direction.none;
    Direction m_swipeDirection = Direction.none;


    float m_timeToNextDrag;
    float m_timeToNextSwipe;

    [Range(0.05f, 1f)]
    float m_minTimeToDrag = 0.15f;
    [Range(0.05f, 1f)]
    float m_minTimeToSwipe = 0.3f;

    bool m_didTap = false;


    void OnEnable()
    {
        TouchController.DragEvent += DragHandler;
        TouchController.SwipeEvent += SwipeHandler;
        TouchController.TapEvent += TapHandler;
    }

    void OnDisable()
    {
        TouchController.DragEvent -= DragHandler;
        TouchController.SwipeEvent -= SwipeHandler;
        TouchController.TapEvent -= TapHandler;
    }

    void Start()
    {
        //m_timeToNextKey = Time.time;

        m_timeToNextKeyLeftRight = Time.time + m_keyRepeatRateLeftRight;
        m_timeToNextKeyRotate = Time.time + m_keyRepeatRateRotate;
        m_timeToNextKeyDown = Time.time + m_keyRepeatRateDown;

        m_gameBoard = GameObject.FindWithTag("Board").GetComponent<Board>();
        m_spawner = GameObject.FindWithTag("Spawner").GetComponent<Spawner>();
        m_scoreManager = GameObject.FindObjectOfType<ScoreManager>();
        m_soundManager = GameObject.FindObjectOfType<SoundManager>();
        m_ghost = GameObject.FindObjectOfType<Ghost>();
        m_holder = GameObject.FindObjectOfType<Holder>();

        if (!m_gameBoard || !m_spawner || !m_soundManager || !m_scoreManager)
        {
            Debug.Log(
                "ERROR board ,spawner, or soundManager not present"
            );
        }

        if (m_diagnosticText)
        {
            m_diagnosticText.text = "";
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


        if (m_pausePanel)
        {
            m_pausePanel.SetActive(false);
        }

        m_dropIntervalModded = m_dropInterval;

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

        if (m_ghost)
        {
            m_ghost.Reset();
        }

        if (m_holder)
        {
            m_holder.m_canRelease = true;
        }

        m_activeShape = m_spawner.SpawnShape();

        m_timeToNextKeyLeftRight = Time.time;
        m_timeToNextKeyRotate = Time.time;
        m_timeToNextKeyDown = Time.time;

        m_gameBoard.StartCoroutine("ClearAllRows");

        playSound(m_soundManager.m_dropSound);

        if (m_gameBoard.m_completedRows > 0)
        {
            m_scoreManager.ScoreLines(m_gameBoard.m_completedRows);

            if (m_scoreManager.m_didLevelUp)
            {
                playSound(m_soundManager.m_levelUpVocalClip);
                m_dropIntervalModded = m_dropInterval - Mathf.Clamp(
                    (((float)m_scoreManager.m_level - 1) * 0.1f), 0.1f, 1f);
            }
            else
            {

                if (m_gameBoard.m_completedRows > 1)
                {
                    AudioClip randomVocal = m_soundManager.GetRandomClip(m_soundManager.m_vocalClips);
                    playSound(randomVocal);
                }

            }
            playSound(m_soundManager.m_clearRowSound);
        }
    }

    void MoveRight()
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

    void MoveLeft()
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

    void MoveDown()
    {
        m_timeToDrop = Time.time + m_dropIntervalModded;
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

    void Rotate()
    {
        // m_activeShape.RotateRight();
        m_activeShape.RotateClockwise(m_clockwise);
        m_timeToNextKeyRotate = Time.time + m_keyRepeatRateRotate;

        if (!m_gameBoard.IsValidPosition(m_activeShape))
        {
            // m_activeShape.RotateLeft();
            m_activeShape.RotateClockwise(!m_clockwise);
            playSound(m_soundManager.m_errorSound);
        }
        else
        {
            playSound(m_soundManager.m_moveSound, 0.5f);
        }
    }


    void PlayerInput()
    {
        if ((Input.GetButton("MoveRight") && Time.time > m_timeToNextKeyLeftRight)
                    || Input.GetButtonDown("MoveRight"))
        {
            MoveRight();
        }
        else if ((Input.GetButton("MoveLeft") && Time.time > m_timeToNextKeyLeftRight)
                  || Input.GetButtonDown("MoveLeft"))
        {
            MoveLeft();
        }
        else if (Input.GetButtonDown("Rotate") && Time.time > m_timeToNextKeyRotate)
        {
            Rotate();
        }
        else if ((Input.GetButton("MoveDown")
            && Time.time > m_timeToNextKeyDown)
            || Time.time > m_timeToDrop)
        {
            MoveDown();
        }

        #region Touch controllers
        else if ((m_swipeDirection == Direction.right && Time.time > m_timeToNextSwipe)
                || (m_dragDirection == Direction.right && Time.time > m_timeToNextDrag))
        {
            MoveRight();
            m_timeToNextDrag = Time.time + m_minTimeToDrag;
            m_timeToNextSwipe = Time.time + m_minTimeToSwipe;
        }

        else if ((m_swipeDirection == Direction.left && Time.time > m_timeToNextSwipe)
                || (m_dragDirection == Direction.left && Time.time > m_timeToNextDrag))
        {
            MoveLeft();
            m_timeToNextDrag = Time.time + m_minTimeToDrag;
            m_timeToNextSwipe = Time.time + m_minTimeToSwipe;
        }

        else if ((m_dragDirection == Direction.up && Time.time > m_timeToNextSwipe)
                || m_didTap)
        {
            Rotate();
            m_timeToNextSwipe = Time.time + m_minTimeToSwipe;
        }

        else if ((m_dragDirection == Direction.down && Time.time > m_timeToNextDrag))
        {
            MoveDown();
        }
        #endregion

        else if (Input.GetButtonDown("ToggleRotate"))
        {
            ToggleRotateDirection();
        }
        else if (Input.GetButtonDown("Pause"))
        {
            TogglePause();
        }
        else if (Input.GetButtonDown("Hold"))
        {
            Hold();
        }
        m_dragDirection = Direction.none;
        m_swipeDirection = Direction.none;
        m_didTap = false;
    }

    // Update is called once per frame
    void Update()
    {
        // no spawner or no gameboard no game
        if (!m_gameBoard || !m_spawner || !m_activeShape || m_gameOver || !m_soundManager || !m_scoreManager)
        {
            return;
        }

        PlayerInput();
    }

    [System.Obsolete]
    public void Restart()
    {
        Application.LoadLevel(Application.loadedLevel);
        Time.timeScale = 1;
    }

    void GameOver()
    {
        m_activeShape.MoveUp();

        if (m_gameOverPanel)
        {
            m_gameOverPanel.SetActive(true);
        }
        playSound(m_soundManager.m_gameOverSound);
        playSound(m_soundManager.m_gameOverVocalClip);
        m_gameOver = true;
    }

    public void ToggleRotateDirection()
    {
        m_clockwise = !m_clockwise;

        if (m_rotateIconToggle)
        {
            m_rotateIconToggle.ToggleIcon(m_clockwise);
        }
    }

    public void TogglePause()
    {
        if (m_gameOver)
        {
            return;
        }

        m_isPaused = !m_isPaused;
        if (m_pausePanel)
        {
            m_pausePanel.SetActive(m_isPaused);
            if (m_soundManager)
            {
                m_soundManager.m_musicSource.volume =
                    m_isPaused ? m_soundManager.m_musicVolume * 0.25f : m_soundManager.m_musicVolume;
            }

            Time.timeScale = m_isPaused ? 0 : 1;
        }
    }

    public void Hold()
    {
        if (!m_holder)
        {
            return;
        }

        if (!m_holder.m_heldShape)
        {
            m_holder.Catch(m_activeShape);
            m_activeShape = m_spawner.SpawnShape();
            playSound(m_soundManager.m_holdSound);
        }
        else if (m_holder.m_canRelease)
        {
            Shape shape = m_activeShape;
            m_activeShape = m_holder.Release();
            m_activeShape.transform.position = m_spawner.transform.position;
            m_holder.Catch(shape);
            playSound(m_soundManager.m_holdSound);
        }
        else
        {
            playSound(m_soundManager.m_errorSound);
        }

        if (m_ghost)
        {
            m_ghost.Reset();
        }
    }

    void LateUpdate()
    {
        if (m_ghost)
        {
            m_ghost.DrawGhost(m_activeShape, m_gameBoard);
        }
    }

    void DragHandler(Vector2 swipeMovement)
    {
        // if (m_diagnosticText)
        // {
        //     m_diagnosticText.text = "Swipe Detected";
        // }
        m_dragDirection = GetDirection(swipeMovement);
    }

    void SwipeHandler(Vector2 swipeMovement)
    {
        // if (m_diagnosticText)
        // {
        //     m_diagnosticText.text = "";
        // }
        m_swipeDirection = GetDirection(swipeMovement);

    }

    void TapHandler(Vector2 tapMovement)
    {
        m_didTap = true;
    }

    Direction GetDirection(Vector2 swipeMovement)
    {
        Direction swipeDir = Direction.none;
        if (Mathf.Abs(swipeMovement.x) > Mathf.Abs(swipeMovement.y))
        {
            swipeDir = (swipeMovement.x >= 0) ? Direction.right : Direction.left;
        }
        else
        {
            swipeDir = (swipeMovement.y >= 0) ? Direction.up : Direction.down;
        }

        return swipeDir;
    }


}

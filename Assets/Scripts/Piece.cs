using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public int rotationIndex { get; private set; }

    public float stepDelay = 1f;
    public float moveDelay = 0.1f;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float moveTime;
    private float lockTime;

    private bool moveLeft;
    private bool moveRight;
    private bool isHoldingDown;
    private bool rotateLeft;
    private bool rotateRight;
    private bool hardDrop;
    private bool restart;

    public Button leftButton;
    public Button rightButton;
    public Button downButton;
    public Button rotateLeftButton;
    public Button rotateRightButton;
    public Button hardDropButton;
    public Button restartButton;

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        this.data = data;
        this.board = board;
        this.position = position;

        rotationIndex = 0;
        stepTime = Time.time + stepDelay;
        moveTime = Time.time + moveDelay;
        lockTime = 0f;

        if (cells == null)
        {
            cells = new Vector3Int[data.cells.Length];
        }

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = (Vector3Int)data.cells[i];
        }

        leftButton.onClick.AddListener(() => moveLeft = true);
        rightButton.onClick.AddListener(() => moveRight = true);
        rotateLeftButton.onClick.AddListener(() => rotateLeft = true);
        rotateRightButton.onClick.AddListener(() => rotateRight = true);
        hardDropButton.onClick.AddListener(() => hardDrop = true);
        restartButton.onClick.AddListener(() => restart = true);
        
        EventTrigger trigger = downButton.gameObject.AddComponent<EventTrigger>();
        
        var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener((data) => { isHoldingDown = true; });
        trigger.triggers.Add(pointerDown);

        var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((data) => { isHoldingDown = false; });
        trigger.triggers.Add(pointerUp);
    }

    private void Update()
    {
        board.Clear(this);

        lockTime += Time.deltaTime;

        HandleUIInputs();

        if (isHoldingDown)
        {
            if (Time.time > moveTime)
            {
                Move(Vector2Int.down);
                moveTime = Time.time + moveDelay;
            }
        }
        
        if (Time.time > stepTime)
        {
            Step();
        }

        board.Set(this);
    }

    private void HandleUIInputs()
    {
        if (restart)
        {
            board.RestartGame();
            restart = false;
        }

        if (moveLeft)
        {
            Move(Vector2Int.left);
            moveLeft = false;
        }

        if (moveRight)
        {
            Move(Vector2Int.right);
            moveRight = false;
        }

        if (rotateLeft)
        {
            Rotate(-1);
            rotateLeft = false;
        }

        if (rotateRight)
        {
            Rotate(1);
            rotateRight = false;
        }

        if (hardDrop)
        {
            HardDrop();
            hardDrop = false;
        }
    }

    private void Step()
    {
        stepTime = Time.time + stepDelay;

        Move(Vector2Int.down);

        if (lockTime >= lockDelay)
        {
            Lock();
        }
    }

    private void HardDrop()
    {
        while (Move(Vector2Int.down))
        {
            continue;
        }

        Lock();
    }

    private void Lock()
    {
        board.Set(this);
        board.ClearLines();
        board.SpawnPiece();
    }

    private bool Move(Vector2Int translation)
    {
        Vector3Int newPosition = position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;

        bool valid = board.IsValidPosition(this, newPosition);

        if (valid)
        {
            position = newPosition;
            moveTime = Time.time + moveDelay;
            lockTime = 0f;
        }

        return valid;
    }

    private void Rotate(int direction)
    {
        int originalRotation = rotationIndex;

        rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        ApplyRotationMatrix(direction);

        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }
    
    public void Clear()
    {
        board.Clear(this);
    }

    private void ApplyRotationMatrix(int direction)
    {
        float[] matrix = Data.RotationMatrix;

        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 cell = cells[i];

            int x, y;

            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;

                default:
                    x = Mathf.RoundToInt((cell.x * matrix[0] * direction) + (cell.y * matrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * matrix[2] * direction) + (cell.y * matrix[3] * direction));
                    break;
            }

            cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);

        for (int i = 0; i < data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];

            if (Move(translation))
            {
                return true;
            }
        }

        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;

        if (rotationDirection < 0)
        {
            wallKickIndex--;
        }

        return Wrap(wallKickIndex, 0, data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min)
        {
            return max - (min - input) % (max - min);
        }
        else
        {
            return min + (input - min) % (max - min);
        }
    }
}
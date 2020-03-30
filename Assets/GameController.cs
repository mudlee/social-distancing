using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameController : MonoBehaviour
{
    public GameObject _grannyPrefab;
    public GameObject[] _shops;
    public GameObject _grannyInfo;
    public GameObject _movePointsText;
    public GameObject _readyText;
    public GameObject _notReadyText;
    public GameObject _startShoppingButton;
    public GameObject _infectionRateText;
    public GameObject _tip;
    private GameObject _selectedGranny;
    private List<GameObject> _grannies = new List<GameObject>();
    private int _patientZero;
    private bool _shopping;

    void Start()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("House");
        _patientZero = Random.Range(0, spawnPoints.Length);

        for (var i = 0; i < spawnPoints.Length; i++)
        {
            var granny = Instantiate(_grannyPrefab);
            granny.transform.position = new Vector3(
                spawnPoints[i].transform.position.x,
                spawnPoints[i].transform.position.y - 1,
                spawnPoints[i].transform.position.z
            );
            granny.name = string.Format("Granny-{0}", GrannyController.GRANNY_COUNTER++);
            if (i == _patientZero)
            {
                granny.GetComponent<GrannyController>().Infect();
            }

            _grannies.Add(granny);
        }

        EventManager.StartListening(EventManager.Event.GRANNY_INFECTED, () => {
            UpdateInfectionRate();
        });
        UpdateInfectionRate();

        Debug.Log(string.Format("Patient Zero is {0}", _patientZero));
        Debug.Log(string.Format("Number of Shops: {0}", _shops.Length));
        Debug.Log(string.Format("Number of Grannies: {0}", _grannies.Count));
    }

    void Update()
    {
        ListenInputs();
        WachResult();
    }

    private void FixedUpdate()
    {
        ListenMouseClick();
    }

    public void RemoveTip()
    {
        Destroy(_tip);
    }

    public void StartShopping()
    {
        _startShoppingButton.SetActive(false);
        _grannies.ForEach(g => g.GetComponent<GrannyController>().BeginMove(_shops.Length));
        _shopping = true;
    }

    private void WachResult()
    {
        if(!_shopping)
        {
            return;
        }

        var numOfShopping = _grannies.Where(g => g.GetComponent<GrannyController>().IsShopping()).ToList().Count;
        if(numOfShopping > 0)
        {
            return;
        }

        if(CalculateInfectionRate() > 60f)
        {
            SceneManager.LoadScene("GameOver");
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    private void UpdateInfectionRate()
    {
        
        _infectionRateText.GetComponent<TextMeshProUGUI>().SetText
        (
            "INFECTION RATE: " + CalculateInfectionRate().ToString() + "%"
        );
    }

    private double CalculateInfectionRate()
    {
        var numberOfInfected = _grannies.Where(g => g.GetComponent<GrannyController>().IsInfected()).ToList().Count;
        return System.Math.Round(((float)numberOfInfected / _grannies.Count * 100f), 1);
    }

    private void ListenMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var mousePos2D = new Vector2(mousePos.x, mousePos.y);
            var hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            var grannyClicked = hit.collider != null && hit.collider.CompareTag(GrannyController.TAG);
            var roadClicked = hit.collider != null && hit.collider.CompareTag("Road");
            var shopClicked = hit.collider != null && hit.collider.CompareTag("Shop");
            var grannySelected = _selectedGranny != null;

            if (roadClicked && grannySelected)
            {
                Debug.Log("Road clicked with a selected granny");
                var granny = _selectedGranny.GetComponent<GrannyController>();
                var remainingMoves = GetMaxMovementPoints() - granny.GetPathLength();
                if (remainingMoves == 0)
                {
                    return;
                }
                granny.QueueDot(mousePos2D, null);
                UpdateGrannyInfo(granny);
            }
            else if(shopClicked && grannySelected)
            {
                Debug.Log("Shop clicked with a selected granny");
                var granny = _selectedGranny.GetComponent<GrannyController>();
                var remainingMoves = GetMaxMovementPoints() - granny.GetPathLength();
                if (remainingMoves == 0)
                {
                    return;
                }
                granny.QueueDot(mousePos2D, hit.collider.GetHashCode());
                UpdateGrannyInfo(granny);
            }
            else if(grannyClicked)
            {
                Debug.Log("Granny clicked");
                var newGrannySelected = hit.collider.GetComponent<GrannyController>();
                if (grannySelected)
                {
                    _selectedGranny.GetComponent<GrannyController>().Selected(false);
                }

                newGrannySelected.Selected(true);
                _selectedGranny = hit.collider.gameObject;
                _grannyInfo.SetActive(true);

                UpdateGrannyInfo(newGrannySelected);
            }
            else if(grannySelected)
            {
                _grannyInfo.SetActive(false);
                _selectedGranny.GetComponent<GrannyController>().Selected(false);
                _selectedGranny = null;
            }
        }
    }

    private void ListenInputs()
    {
        if(Input.GetKeyDown(KeyCode.D) && _selectedGranny!=null)
        {
            var granny = _selectedGranny.GetComponent<GrannyController>();
            if(granny.GetPathLength() == 0)
            {
                return;
            }

            granny.RemoveLastPlacedDot();
            var remainingMoves = GetMaxMovementPoints() - granny.GetPathLength();
            _movePointsText.GetComponent<TextMeshProUGUI>().SetText("REMAINING MOVES - " + remainingMoves);
            UpdateGrannyInfo(granny);
        }
    }

    private int GetMaxMovementPoints()
    {
        return _shops.Length * 2 + 2;
    }

    private void UpdateGrannyInfo(GrannyController granny)
    {
        var remainingMoves = GetMaxMovementPoints() - granny.GetPathLength();
        _movePointsText.GetComponent<TextMeshProUGUI>().SetText("REMAINING MOVES - " + remainingMoves);

        var grannyReady = granny.ReadyToMove(_shops.Length);
        _readyText.SetActive(grannyReady);
        _notReadyText.SetActive(!grannyReady);

        CheckIfAllGranniesAreReady();
    }

    private void CheckIfAllGranniesAreReady()
    {
        var numOfReady = _grannies.Where(g => g.GetComponent<GrannyController>().ReadyToMove(_shops.Length)).ToList().Count;
        _startShoppingButton.SetActive(numOfReady == _grannies.Count);
    }
}

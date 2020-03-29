using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SAP2D;

public class GrannyController : MonoBehaviour
{
    public static string TAG = "Granny";
    public static int GRANNY_COUNTER = 0;
    public GameObject _moveDotPrefab;
    public Sprite _selectedSprite;
    public Sprite _idleSprite;

    private static int DOT_COUNTER = 0;
    private static float SPEED = 2.5f;
    
    private int _id;
    private List<DotEntry> _dots = new List<DotEntry>();
    private SAP2DAgent _sAP2DAgent;
    private bool _onItsWay;
    private bool _infected;
    private Animator _animator;

    void Start()
    {
        _id = GRANNY_COUNTER;
        _sAP2DAgent = GetComponent<SAP2DAgent>();
        _sAP2DAgent.MovementSpeed = SPEED;
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (_onItsWay && _sAP2DAgent.Target != null)
        {
            if(Vector3.Distance(transform.position, _sAP2DAgent.Target.position) < 1f)
            {
                var dotToBeRemoved = _dots[0];
                _dots.RemoveAt(0);
                _sAP2DAgent.CanSearch = false;
                Move();
                dotToBeRemoved.Dot.SetActive(false);
            }
        }
    }

    public bool IsInfected()
    {
        return _infected;
    }

    public int GetPathLength()
    {
        return _dots.Count;
    }

    public void BeginMove(int expectedShops)
    {
        if (_dots.Count == 0 || !ReadyToMove(expectedShops))
        {
            return;
        }
        Selected(false);
        _animator.SetBool("Walking", true);
        _onItsWay = true;
        Debug.Log(string.Format("Granny-{0} started moving as infected: {1}",_id,_infected));
        Move();
    }

    public bool IsShopping()
    {
        return _onItsWay;
    }

    public bool ReadyToMove(int expectedShops)
    {
        return expectedShops == _dots.Select(d => d.ShopId).Distinct().Where(id => id != null).ToList().Count;
    }

    public void QueueDot(Vector2 at, int? shopId)
    {
        var dot = Instantiate(_moveDotPrefab);
        dot.transform.position = at;
        dot.name = string.Format("Granny-{0}-path-{1}", _id, ++DOT_COUNTER);
        _dots.Add(new DotEntry(
            dot.transform,
            dot,
            shopId
        ));
    }

    public void RemoveLastPlacedDot()
    {
        var dotEntry = _dots[_dots.Count - 1];
        Destroy(dotEntry.Dot);
        _dots.RemoveAt(_dots.Count - 1);
        --DOT_COUNTER;
    }

    public void Selected(bool selected)
    {
        _animator.SetBool("Selected", selected);
    }

    public void Infect()
    {
        _infected = true;
        EventManager.TriggerEvent(EventManager.Event.GRANNY_INFECTED);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(_infected && collision.CompareTag(TAG))
        {
            collision.GetComponent<GrannyController>().Infect();
        }
    }

    private void Move()
    {
        if (_dots.Count == 0)
        {
            _onItsWay = false;
            _animator.SetBool("Walking", false);
            return;
        }
        _sAP2DAgent.CanSearch = true;
        _sAP2DAgent.Target = _dots[0].Position;
    }

    private class DotEntry
    {
        public Transform Position { get; }
        public GameObject Dot { get; }
        public int? ShopId { get; }

        public DotEntry(Transform position, GameObject dot, int? shopId)
        {
            this.Position = position;
            this.Dot = dot;
            this.ShopId = shopId;
        }
    }
}

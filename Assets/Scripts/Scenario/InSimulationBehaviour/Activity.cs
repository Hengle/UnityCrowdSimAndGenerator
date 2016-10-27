using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Animations;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SphereCollider))]
public class Activity : MonoBehaviour
{
    private const string IDLE_STATE_NAME = "Standing";
    private Animator _animator;
    private SphereCollider _sphereCollider;
    private string _paramName;
    private string _blendParam;
    private bool _complexAction;
    private bool _canExecuteComplexAction;

    public List<GameObject> _otherRequiredAgents;

    private bool[] _requiredAgentsNearbyCheck;
    private float _exitTime;
    private float _elapsedTimeCounter;
    private bool _isFinished;
    private NavMeshAgent _navMeshAgent;
    private Bounds _actionBounds;
    private GameObject _actionArena;

    private string _nameToDisplay;
    private int _levelIndex;

    public DynamicAnimationState _dynamicAnimationState;

    public bool IsFinished
    {
        get
        {
            return _isFinished;
        }
    }

    public float ExitTime
    {
        get
        {
            return _exitTime;
        }
        set
        {
            _exitTime = value;
        }
    }

    public string ParamName
    {
        get
        {
            return _paramName;
        }
        set
        {
            _paramName = value;
            string[] name = _paramName.Split('@');
            _nameToDisplay = name[1];
            _isFinished = false;
            _elapsedTimeCounter = 0.0f;
            GetComponent<NavMeshAgent>().obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            _dynamicAnimationState = new DynamicAnimationState(_animator, _paramName);
            _exitTime = _dynamicAnimationState.Length;
        }
    }

    public List<GameObject> OtherAgents
    {
        get
        {
            return _otherRequiredAgents;
        }

        set
        {
            _otherRequiredAgents = value;
            if (_otherRequiredAgents != null)
            {
                _otherRequiredAgents.Remove(gameObject);
                _requiredAgentsNearbyCheck = new bool[_otherRequiredAgents.Count];
                _complexAction = true;
                _canExecuteComplexAction = false;
                _sphereCollider.enabled = true;
            }
            else
            {
                _complexAction = false;
            }
        }
    }
    public string BlendParameter
    {
        get
        {
            return _blendParam;
        }
        set
        {
            _blendParam = value;
        }
    }

    public Bounds ActionBounds
    {
        get
        {
            return _actionBounds;
        }

        set
        {
            _actionBounds = value;
        }
    }

    public string NameToDisplay
    {
        get
        {
            if (_complexAction && !_canExecuteComplexAction)
            {
                _nameToDisplay = IDLE_STATE_NAME;
            }
            return _nameToDisplay;
        }
    }

    public int LevelIndex
    {
        get
        {
            return _levelIndex;
        }

        set
        {
            _levelIndex = value;
        }
    }

    public string ActorName
    {
        get
        {
            return name;
        }
    }

    public string MocapId
    {
        get
        {
            string[] name = _paramName.Split('@');
            return name[0];
        }
    }

    public bool IsComplex
    {
        get
        {
            return _complexAction && _canExecuteComplexAction && _nameToDisplay != IDLE_STATE_NAME;
        }
    }

    private void CreateLocalAnimatorControllerCopy()
    {
        AnimatorController currentController = _animator.runtimeAnimatorController as AnimatorController;
        AnimatorController newController = currentController.Clone();
        newController.name = _animator.gameObject.name + "LocalController";
        _animator.runtimeAnimatorController = newController;
    }

    private void CreateActionArena()
    {
        if (_actionArena == null)
        {
            _actionArena = new GameObject();
            NavMeshObstacle obstacle = _actionArena.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.center = gameObject.transform.position;
            obstacle.size = _actionBounds.size;
        }
    }

    private void DeleteActionArena()
    {
        if (_actionArena != null)
        {
            GameObject.DestroyImmediate(_actionArena);
            _actionArena = null;
        }
    }

    void Awake()
    {
        _animator = gameObject.GetComponent<Animator>();
        _sphereCollider = gameObject.GetComponent<SphereCollider>();
        _navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
        _sphereCollider.radius = 3.0f;
        _sphereCollider.isTrigger = true;
        _sphereCollider.enabled = false;
        _isFinished = true;

        CreateLocalAnimatorControllerCopy();
    }

    void Update()
    {
        if (!IsFinished)
        {

            if (!_complexAction)
            {
                if (ExitTime > 0.0f && _elapsedTimeCounter <= ExitTime)
                {
                    if (_elapsedTimeCounter >= ExitTime * 0.9f)
                    {
                        DeleteActionArena();
                    }
                    else
                    {
                        CreateActionArena();
                    }

                    _navMeshAgent.enabled = false;
                    _elapsedTimeCounter += Time.deltaTime;
                    _dynamicAnimationState.EnterState();

                }

                if (ExitTime > 0.0f && _elapsedTimeCounter >= ExitTime)
                {
                    _isFinished = true;
                    _dynamicAnimationState.ExitState();
                    DeleteActionArena();
                    _navMeshAgent.enabled = true;
                }
            }
            else
            {
                if (!_canExecuteComplexAction)
                {
                    _nameToDisplay = IDLE_STATE_NAME;
                    _canExecuteComplexAction = true;
                    foreach (bool agentNearbyCheck in _requiredAgentsNearbyCheck)
                    {
                        if (!agentNearbyCheck)
                        {
                            _canExecuteComplexAction = false;
                            break;
                        }
                    }
                }
                else
                {
                    string[] name = _paramName.Split('@');
                    _nameToDisplay = name[1];
                    if (ExitTime > 0.0f && _elapsedTimeCounter <= ExitTime)
                    {
                        if (_elapsedTimeCounter >= ExitTime * 0.9f)
                        {
                            DeleteActionArena();
                        }
                        else
                        {
                            CreateActionArena();
                        }

                        _navMeshAgent.enabled = false;
                        _elapsedTimeCounter += Time.deltaTime;
                        _dynamicAnimationState.EnterState();
                    }

                    if (ExitTime > 0.0f && _elapsedTimeCounter >= ExitTime)
                    {
                        _isFinished = true;
                        _dynamicAnimationState.ExitState();
                        DeleteActionArena();
                        _navMeshAgent.enabled = true;
                    }
                }
            }
            //Debug.Log(string.Format("{0} {1} {2} {3}", LevelIndex, MocapId, ActorName, NameToDisplay));
        }
        else
        {
            GetComponent<NavMeshAgent>().obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            _elapsedTimeCounter = 0.0f;
            _otherRequiredAgents = null;
            _complexAction = false;
            _sphereCollider.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetType() == typeof(SphereCollider))
        {
            if (!IsFinished)
            {
                if (_complexAction)
                {
                    for (int i = 0; i < _otherRequiredAgents.Count; i++)
                    {
                        if (other.gameObject == _otherRequiredAgents[i])
                        {
                            _requiredAgentsNearbyCheck[i] = true;
                        }
                    }
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetType() == typeof(SphereCollider))
        {
            if (!IsFinished && _complexAction)
            {
                for (int i = 0; i < _otherRequiredAgents.Count; i++)
                {
                    if (other.gameObject == _otherRequiredAgents[i])
                    {
                        _requiredAgentsNearbyCheck[i] = false;
                    }
                }
            }
        }
    }
}

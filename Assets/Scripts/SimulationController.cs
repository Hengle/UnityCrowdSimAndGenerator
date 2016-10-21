using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CrowdController))]
[RequireComponent(typeof(WeatherConditions))]
[RequireComponent(typeof(Screenshooter))]
[RequireComponent(typeof(CamerasController))]
public class SimulationController : MonoBehaviour
{
    public int Repeats;
    [Header("Scenario")]
    public string ScenarioFile;
    public int SimultaneousScenarioInstances;
    [Header("Tracking")]
    public bool Tracking;
    public int SessionLength = 1;
    [Header("Results")]
    public string ScreenshotsDirectory = "D:/Screenshots";
    [Header("Testing")]
    public bool LoadFromConfig;
    public bool MarkWithPlanes;
    public bool Close;

    private CrowdController _crowdController;
    private SequencesCreator _sequenceCreator;
    private Screenshooter _screenshooter;
    private List<SequenceController> _actorsSequencesControllers;
    private int _repeatsCounter;
    //private float _elapsedTimeCounter;
    private int _elapsedTimeCounter;
    private bool _instanceFinished;
    private bool _screnshooterActive;
    private bool _screenshotBufferFull = false;
    private string[] _actorsNames;
    private List<GameObject> _actors;

    void Start()
    {
        _crowdController = GetComponent<CrowdController>();
        _sequenceCreator = new SequencesCreator();
        _screenshooter = FindObjectOfType<Screenshooter>();
        WeatherConditions weather = GetComponent<WeatherConditions>();
        if (LoadFromConfig)
        {
            XmlConfigReader.ParseXmlConfig(Application.dataPath + "/config.xml");

            weather.Time = XmlConfigReader.Data.DayTime;
            weather.Conditions = XmlConfigReader.Data.WeatherConditions;

            _crowdController.CreatePrefabs = true;
            _crowdController.LoadAgentsFromResources = true;
            _crowdController.AgentsFilter = XmlConfigReader.Data.Models;
            _crowdController.MaxPeople = XmlConfigReader.Data.MaxPeople;
            _crowdController.ActionsFilter = XmlConfigReader.Data.ActionsFilter;

            Tracking = XmlConfigReader.Data.Tracking;
            ScenarioFile = XmlConfigReader.Data.ScenarioFile;
            SessionLength = XmlConfigReader.Data.Length > 1 ? XmlConfigReader.Data.Length : 1;

            Repeats = XmlConfigReader.Data.Repeats > 1 ? XmlConfigReader.Data.Repeats : 1;
            SimultaneousScenarioInstances = XmlConfigReader.Data.Instances > 1 ? XmlConfigReader.Data.Instances : 1;

            ScreenshotsDirectory = XmlConfigReader.Data.ResultsDirectory;
            //_screenshooter.TakeScreenshots = true;
            //_screenshooter.MarkAgentsOnScreenshots = XmlConfigReader.Data.BoundingBoxes;

            _screenshooter.SetParams(true, XmlConfigReader.Data.BoundingBoxes);
            _screenshooter.ResWidth = XmlConfigReader.Data.ResolutionWidth;
            _screenshooter.ResHeight = XmlConfigReader.Data.ResolutionHeight;
            _screenshooter.ChangeFrameRate(XmlConfigReader.Data.FrameRate);
            _screenshooter.ScreenshotLimit = XmlConfigReader.Data.BufferSize;

            Close = true;
            MarkWithPlanes = false;
            GetComponent<CamerasController>().enabled = false;
        }
        weather.GenerateWeatherConditions();
        if (GetComponent<Lighting>() != null)
        {
            GetComponent<Lighting>().SetSampleSceneLighting();
        }

        SessionLength *= _screenshooter.FrameRate;
        if (!Tracking)
        {
            XmlScenarioReader.ParseXmlWithScenario(ScenarioFile);
            _actorsNames = GetActorsNames(XmlScenarioReader.ScenarioData);
            _actorsSequencesControllers = new List<SequenceController>();
        }

        _screnshooterActive = _screenshooter.TakeScreenshots;
        if (_screnshooterActive)
        {
            string dir = string.Format("/Session-{0:yyyy-MM-dd_hh-mm-ss-tt}", System.DateTime.Now);
            ScreenshotsDirectory += dir;
        }
        _screenshooter.TakeScreenshots = false;

        Invoke("StartInstanceOfSimulation", 0.5f);
    }

    void Update()
    {
        if (!_instanceFinished)
        {
            _elapsedTimeCounter++;

            if (Tracking)
            {
                if (_elapsedTimeCounter >= SessionLength)
                {
                    EndInstanceOfSimulation();
                }
            }
            else
            {
                if (_actorsSequencesControllers.Count > 0)
                {
                    bool endInstance = true;
                    foreach (SequenceController agentScenario in _actorsSequencesControllers)
                    {
                        if (!agentScenario.IsFinished)
                        {
                            endInstance = false;
                            break;
                        }
                    }
                    if (endInstance)
                    {
                        EndInstanceOfSimulation();
                    }
                }
                if (_elapsedTimeCounter >= SessionLength * 5.0f)
                {
                    EndInstanceOfSimulation();
                    Debug.Log("Aborting sequence");
                }
            }

            if (_screenshotBufferFull)
            {
                string path = ScreenshotsDirectory + "/Take_" + _repeatsCounter;

                Debug.Log("Screenshot buffer full! Saving to: " + path);
                _screenshooter.SaveScreenshotsAtDirectory(path);
                _screenshotBufferFull = false;
            }
        }
    }

    private void StartInstanceOfSimulation()
    {
        _crowdController.GenerateCrowd();
        if (!Tracking)
        {
            _actors = CreateActorsFromCrowd(SimultaneousScenarioInstances, _actorsNames);
            _sequenceCreator.RawInfoToListPerAgent(XmlScenarioReader.ScenarioData);
            _sequenceCreator.Agents = _actors;
            _sequenceCreator.MarkActions = MarkWithPlanes;
            _sequenceCreator.Crowd = false;
            _sequenceCreator.ShowSequenceOnConsole = true;
            _actorsSequencesControllers = _sequenceCreator.GenerateInGameSequences(SimultaneousScenarioInstances, out SessionLength);
            SessionLength *= _screenshooter.FrameRate;
        }
        _sequenceCreator.MarkActions = false;
        _sequenceCreator.Crowd = true;
        _sequenceCreator.ShowSequenceOnConsole = false;
        foreach (GameObject agent in _crowdController.Crowd.Where(x => x.tag == "Crowd").ToList())
        {
            _sequenceCreator.RawInfoToListPerAgent(_crowdController.PrepareActions(agent));
            _sequenceCreator.Agents = new List<GameObject> { agent };
            int temp;
            _sequenceCreator.GenerateInGameSequences(1, out temp);
        }

        _screenshooter.Annotator = new Annotator(_crowdController.Crowd);
        _screenshooter.TakeScreenshots = _screnshooterActive;

        _repeatsCounter++;
        _instanceFinished = false;
        _elapsedTimeCounter = 0;
    }

    private void EndInstanceOfSimulation()
    {
        _instanceFinished = true;

        if (_screnshooterActive)
        {
            _screenshooter.SaveScreenshotsAtDirectory(ScreenshotsDirectory + "/Take_" + _repeatsCounter);
            _screenshooter.TakeScreenshots = false;
        }

        _crowdController.RemoveCrowd();
        StartCoroutine(EndInstance());
    }

    private IEnumerator EndInstance()
    {
        yield return new WaitForSeconds(0.5f);
        if (_repeatsCounter < Repeats)
        {
            StartInstanceOfSimulation();
        }
        else
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            if (Close)
            {
                EditorApplication.Exit(0);
            }
#else
                Application.Quit();
#endif
        }
    }

    public void NotifyScreenshotBufferFull()
    {
        _screenshotBufferFull = true;
    }

    private List<GameObject> CreateActorsFromCrowd(int simultaneousInstances, string[] actorsNames)
    {
        CrowdController crowdController = GetComponent<CrowdController>();
        if (crowdController.MaxPeople < actorsNames.Length * simultaneousInstances)
        {
            crowdController.RemoveCrowd();
            crowdController.MaxPeople = actorsNames.Length * simultaneousInstances;
            crowdController.GenerateCrowd();
        }
        List<GameObject> actors = new List<GameObject>();
        for (int i = 0; i < simultaneousInstances; i++)
        {
            for (int j = 0; j < actorsNames.Length; j++)
            {
                GameObject[] crowd = GameObject.FindGameObjectsWithTag("Crowd");
                int index = Random.Range(0, crowd.Length);
                crowd[index].tag = "ScenarioAgent";
                crowd[index].name = actorsNames[j] + "_" + i;
                crowd[index].GetComponent<NavMeshAgent>().avoidancePriority = 0;
                crowd[index].GetComponent<NavMeshAgent>().stoppingDistance = 0.02f;
                //crowd[index].GetComponent<GenerateDestination>().enabled = false;
                crowd[index].AddComponent<DisplayActivityText>();
                actors.Add(crowd[index]);
                if (MarkWithPlanes)
                {
                    MarkActorWithPlane(crowd[index]);
                }
            }
        }
        return actors;
    }

    private void MarkActorWithPlane(GameObject actor)
    {
        GameObject planeMarkup = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeMarkup.transform.localScale = new Vector3(0.1f, 1.0f, 0.1f);
        planeMarkup.transform.parent = actor.transform;
        planeMarkup.transform.localPosition = new Vector3(0.0f, 0.1f, 0.0f);
        Destroy(planeMarkup.GetComponent<MeshCollider>());
    }

    private string[] GetActorsNames(List<Level> data)
    {
        HashSet<string> hashedActors = new HashSet<string>();
        foreach (Level level in data)
        {
            foreach (Action activity in level.Actions)
            {
                foreach (Actor actor in activity.Actors)
                {
                    hashedActors.Add(actor.Name);
                }
            }
        }
        string[] actors = hashedActors.ToArray();
        return actors;
    }
}


//Uncomment commented and comment ucommented to allow for bulk simulation 

//using UnityEngine;
//using UnityEditor;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;

//[RequireComponent(typeof(CrowdController))]
//[RequireComponent(typeof(WeatherConditions))]
//[RequireComponent(typeof(Screenshooter))]
//[RequireComponent(typeof(CamerasController))]
//public class SimulationController : MonoBehaviour
//{
//    public int Repeats;
//    [Header("Scenario")]
//    public string ScenarioFile;
//    public int SimultaneousScenarioInstances;
//    [Header("Tracking")]
//    public bool Tracking;
//    public int SessionLength = 1;
//    [Header("Results")]
//    public string ScreenshotsDirectory = "D:/Screenshots";
//    [Header("Testing")]
//    public bool LoadFromConfig;
//    public bool MarkWithPlanes;
//    public bool Close;

//    private CrowdController _crowdController;
//    private SequencesCreator _sequenceCreator;
//    private Screenshooter _screenshooter;
//    private List<SequenceController> _actorsSequencesControllers;
//    private int _repeatsCounter;
//    //private float _elapsedTimeCounter;
//    private int _elapsedTimeCounter;
//    private bool _instanceFinished;
//    private bool _screnshooterActive;
//    private bool _screenshotBufferFull = false;
//    private string[] _actorsNames;
//    private List<GameObject> _actors;


//    public struct LukaszToPala
//    {
//        public int Time;
//        public int Weather;
//        public int Crowd;
//        public bool Boxes;

//        public LukaszToPala(int time, int weather, int crowd, bool boxes)
//        {
//            Time = time;
//            Weather = weather;
//            Crowd = crowd;
//            Boxes = boxes;
//        }
//    }
//    public List<LukaszToPala> _lukasze;
//    public int _iter;

//    void Start()
//    {
//        _crowdController = GetComponent<CrowdController>();
//        _sequenceCreator = new SequencesCreator();
//        _screenshooter = FindObjectOfType<Screenshooter>();
//        _lukasze = new List<LukaszToPala>();
//        for (int i = 1; i < 4; i++)
//        {
//            for (int j = 1; j < 6; j++)
//            {
//                for (int k = 0; k < 3; k++)
//                {
//                    for (int l = 0; l < 2; l++)
//                    {
//                        if (l == 0)
//                        {
//                            _lukasze.Add(new LukaszToPala(i, j,25 + k * 65, false));
//                        }
//                        else if (l == 1)
//                        {
//                            _lukasze.Add(new LukaszToPala(i, j, 25 + k * 65, true));
//                        }
//                    }
//                }
//            }
//        }
//        _iter = 0;
//        Autoscreens(_lukasze[_iter]);
//        //WeatherConditions weather = GetComponent<WeatherConditions>();
//        //if (LoadFromConfig)
//        //{
//        //    XmlConfigReader.ParseXmlConfig(Application.dataPath + "/config.xml");

//        //    weather.Time = XmlConfigReader.Data.DayTime;
//        //    weather.Conditions = XmlConfigReader.Data.WeatherConditions;

//        //    _crowdController.CreatePrefabs = true;
//        //    _crowdController.LoadAgentsFromResources = true;
//        //    _crowdController.AgentsFilter = XmlConfigReader.Data.Models;
//        //    _crowdController.MaxPeople = XmlConfigReader.Data.MaxPeople;
//        //    _crowdController.ActionsFilter = XmlConfigReader.Data.ActionsFilter;

//        //    Tracking = XmlConfigReader.Data.Tracking;
//        //    ScenarioFile = XmlConfigReader.Data.ScenarioFile;
//        //    SessionLength = XmlConfigReader.Data.Length > 1 ? XmlConfigReader.Data.Length : 1;

//        //    Repeats = XmlConfigReader.Data.Repeats > 1 ? XmlConfigReader.Data.Repeats : 1;
//        //    SimultaneousScenarioInstances = XmlConfigReader.Data.Instances > 1 ? XmlConfigReader.Data.Instances : 1;

//        //    ScreenshotsDirectory = XmlConfigReader.Data.ResultsDirectory;
//        //    //_screenshooter.TakeScreenshots = true;
//        //    //_screenshooter.MarkAgentsOnScreenshots = XmlConfigReader.Data.BoundingBoxes;

//        //    _screenshooter.SetParams(true, XmlConfigReader.Data.BoundingBoxes);
//        //    _screenshooter.ResWidth = XmlConfigReader.Data.ResolutionWidth;
//        //    _screenshooter.ResHeight = XmlConfigReader.Data.ResolutionHeight;
//        //    _screenshooter.ChangeFrameRate(XmlConfigReader.Data.FrameRate);
//        //    _screenshooter.ScreenshotLimit = XmlConfigReader.Data.BufferSize;

//        //    Close = true;
//        //    MarkWithPlanes = false;
//        //    GetComponent<CamerasController>().enabled = false;
//        //}
//        //weather.GenerateWeatherConditions();
//        //if (GetComponent<Lighting>() != null)
//        //{
//        //    GetComponent<Lighting>().SetSampleSceneLighting();
//        //}

//        //SessionLength *= _screenshooter.FrameRate;
//        //if (!Tracking)
//        //{
//        //    XmlScenarioReader.ParseXmlWithScenario(ScenarioFile);
//        //    _actorsNames = GetActorsNames(XmlScenarioReader.ScenarioData);
//        //    _actorsSequencesControllers = new List<SequenceController>();
//        //}

//        //_screnshooterActive = _screenshooter.TakeScreenshots;
//        //if (_screnshooterActive)
//        //{
//        //    string dir = string.Format("/Session-{0:yyyy-MM-dd_hh-mm-ss-tt}", System.DateTime.Now);
//        //    ScreenshotsDirectory += dir;
//        //}
//        //_screenshooter.TakeScreenshots = false;

//        //Invoke("StartInstanceOfSimulation", 0.5f);
//    }

//    void Update()
//    {
//        if (!_instanceFinished)
//        {
//            _elapsedTimeCounter++;

//            if (Tracking)
//            {
//                if (_elapsedTimeCounter >= SessionLength)
//                {
//                    EndInstanceOfSimulation();
//                }
//            }
//            else
//            {
//                if (_actorsSequencesControllers.Count > 0)
//                {
//                    bool endInstance = true;
//                    foreach (SequenceController agentScenario in _actorsSequencesControllers)
//                    {
//                        if (!agentScenario.IsFinished)
//                        {
//                            endInstance = false;
//                            break;
//                        }
//                    }
//                    if (endInstance)
//                    {
//                        EndInstanceOfSimulation();
//                    }
//                }
//                if (_elapsedTimeCounter >= SessionLength * 5.0f)
//                {
//                    EndInstanceOfSimulation();
//                    Debug.Log("Aborting sequence");
//                }
//            }

//            if (_screenshotBufferFull)
//            {
//                string path = ScreenshotsDirectory + "/Take_" + _repeatsCounter;

//                Debug.Log("Screenshot buffer full! Saving to: " + path);
//                _screenshooter.SaveScreenshotsAtDirectory(path);
//                _screenshotBufferFull = false;
//            }
//        }
//    }

//    private void StartInstanceOfSimulation()
//    {
//        _crowdController.GenerateCrowd();
//        if (!Tracking)
//        {
//            _actors = CreateActorsFromCrowd(SimultaneousScenarioInstances, _actorsNames);
//            _sequenceCreator.RawInfoToListPerAgent(XmlScenarioReader.ScenarioData);
//            _sequenceCreator.Agents = _actors;
//            _sequenceCreator.MarkActions = MarkWithPlanes;
//            _sequenceCreator.Crowd = false;
//            _sequenceCreator.ShowSequenceOnConsole = true;
//            _actorsSequencesControllers = _sequenceCreator.GenerateInGameSequences(SimultaneousScenarioInstances, out SessionLength);
//            SessionLength *= _screenshooter.FrameRate;
//        }
//        _sequenceCreator.MarkActions = false;
//        _sequenceCreator.Crowd = true;
//        _sequenceCreator.ShowSequenceOnConsole = false;
//        foreach (GameObject agent in _crowdController.Crowd.Where(x => x.tag == "Crowd").ToList())
//        {
//            _sequenceCreator.RawInfoToListPerAgent(_crowdController.PrepareActions(agent));
//            _sequenceCreator.Agents = new List<GameObject> { agent };
//            int temp;
//            _sequenceCreator.GenerateInGameSequences(1, out temp);
//        }

//        _screenshooter.Annotator = new Annotator(_crowdController.Crowd);
//        _screenshooter.TakeScreenshots = _screnshooterActive;

//        _repeatsCounter++;
//        _instanceFinished = false;
//        _elapsedTimeCounter = 0;
//    }

//    private void EndInstanceOfSimulation()
//    {
//        _instanceFinished = true;

//        if (_screnshooterActive)
//        {
//            _screenshooter.SaveScreenshotsAtDirectory(ScreenshotsDirectory + "/Take_" + _repeatsCounter);
//            _screenshooter.TakeScreenshots = false;
//        }

//        _crowdController.RemoveCrowd();
//        StartCoroutine(EndInstance());
//    }

//    private IEnumerator EndInstance()
//    {
//        yield return new WaitForSeconds(0.5f);
//        //        if (_repeatsCounter < Repeats)
//        //        {
//        //            StartInstanceOfSimulation();
//        //        }
//        //        else
//        //        {
//        //#if UNITY_EDITOR
//        //            EditorApplication.isPlaying = false;
//        //            if (Close)
//        //            {
//        //                EditorApplication.Exit(0);
//        //            }
//        //#else
//        //                Application.Quit();
//        //#endif
//        //       }
//        if (_iter < _lukasze.Count)
//        {
//            Autoscreens(_lukasze[_iter]);
//        }
//    }

//    public void NotifyScreenshotBufferFull()
//    {
//        _screenshotBufferFull = true;
//    }

//    private List<GameObject> CreateActorsFromCrowd(int simultaneousInstances, string[] actorsNames)
//    {
//        CrowdController crowdController = GetComponent<CrowdController>();
//        if (crowdController.MaxPeople < actorsNames.Length * simultaneousInstances)
//        {
//            crowdController.RemoveCrowd();
//            crowdController.MaxPeople = actorsNames.Length * simultaneousInstances;
//            crowdController.GenerateCrowd();
//        }
//        List<GameObject> actors = new List<GameObject>();
//        for (int i = 0; i < simultaneousInstances; i++)
//        {
//            for (int j = 0; j < actorsNames.Length; j++)
//            {
//                GameObject[] crowd = GameObject.FindGameObjectsWithTag("Crowd");
//                int index = Random.Range(0, crowd.Length);
//                crowd[index].tag = "ScenarioAgent";
//                crowd[index].name = actorsNames[j] + "_" + i;
//                crowd[index].GetComponent<NavMeshAgent>().avoidancePriority = 0;
//                crowd[index].GetComponent<NavMeshAgent>().stoppingDistance = 0.02f;
//                //crowd[index].GetComponent<GenerateDestination>().enabled = false;
//                crowd[index].AddComponent<DisplayActivityText>();
//                actors.Add(crowd[index]);
//                if (MarkWithPlanes)
//                {
//                    MarkActorWithPlane(crowd[index]);
//                }
//            }
//        }
//        return actors;
//    }

//    private void MarkActorWithPlane(GameObject actor)
//    {
//        GameObject planeMarkup = GameObject.CreatePrimitive(PrimitiveType.Plane);
//        planeMarkup.transform.localScale = new Vector3(0.1f, 1.0f, 0.1f);
//        planeMarkup.transform.parent = actor.transform;
//        planeMarkup.transform.localPosition = new Vector3(0.0f, 0.1f, 0.0f);
//        Destroy(planeMarkup.GetComponent<MeshCollider>());
//    }

//    private string[] GetActorsNames(List<Level> data)
//    {
//        HashSet<string> hashedActors = new HashSet<string>();
//        foreach (Level level in data)
//        {
//            foreach (Action activity in level.Actions)
//            {
//                foreach (Actor actor in activity.Actors)
//                {
//                    hashedActors.Add(actor.Name);
//                }
//            }
//        }
//        string[] actors = hashedActors.ToArray();
//        return actors;
//    }

//    private void Autoscreens(LukaszToPala lukasz)
//    {
//        WeatherConditions weather = GetComponent<WeatherConditions>();

//        weather.Time = lukasz.Time;
//        weather.Conditions = lukasz.Weather;

//        _crowdController.CreatePrefabs = true;
//        _crowdController.LoadAgentsFromResources = true;
//        _crowdController.AgentsFilter = "";
//        _crowdController.MaxPeople = lukasz.Crowd;
//        _crowdController.ActionsFilter = "";

//        Tracking = true;
//        ScenarioFile = "";
//        SessionLength = 60;

//        Repeats = 1;
//        SimultaneousScenarioInstances = 1;
//        string time, con, size, box;
//        switch (lukasz.Time)
//        {
//            case 1:
//                time = "Moring";
//                break;
//            case 2:
//                time = "Noon";
//                break;
//            case 3:
//                time = "Evening";
//                break;
//            default:
//                time = "Time";
//                break;
//        }
//        switch (lukasz.Weather)
//        {
//            case 1:
//                con = "Sun";
//                break;
//            case 2:
//                con = "Rain";
//                break;
//            case 3:
//                con = "Snow";
//                break;
//            case 4:
//                con = "Overcast";
//                break;
//            case 5:
//                con = "Fog";
//                break;
//            default:
//                con = "Weather";
//                break;
//        }
//        switch (lukasz.Crowd)
//        {
//            case 25:
//                size = "Small";
//                break;
//            case 90:
//                size = "Medium";
//                break;
//            case 155:
//                size = "Large";
//                break;
//            default:
//                size = "Size";
//                break;
//        }
//        if (lukasz.Boxes)
//        {
//            box = "WithBB";
//        }
//        else
//        {
//            box = "WithoutBB";
//        }

//        ScreenshotsDirectory = string.Format("D:/Screenshots/{0}_{1}_{2}_{3}", time, con, size, box);

//        _screenshooter.SetParams(true, lukasz.Boxes);
//        _screenshooter.ResWidth = 800;
//        _screenshooter.ResHeight = 600;
//        _screenshooter.ChangeFrameRate(24);
//        _screenshooter.ScreenshotLimit = 500;

//        Close = false;
//        MarkWithPlanes = false;
//        GetComponent<CamerasController>().enabled = false;

//        weather.GenerateWeatherConditions();
//        if (GetComponent<Lighting>() != null)
//        {
//            GetComponent<Lighting>().SetSampleSceneLighting();
//        }

//        SessionLength *= _screenshooter.FrameRate;

//        _screnshooterActive = _screenshooter.TakeScreenshots;
//        if (_screnshooterActive)
//        {
//            string dir = string.Format("/Session-{0:yyyy-MM-dd_hh-mm-ss-tt}", System.DateTime.Now);
//            ScreenshotsDirectory += dir;
//        }
//        _screenshooter.TakeScreenshots = false;

//        _iter++;
//        Invoke("StartInstanceOfSimulation", 0.5f);
//    }
//}

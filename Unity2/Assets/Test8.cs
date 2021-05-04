﻿using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using RealtimeBuffer;
using System.Linq;
using TMPro;
using Graphics;
using PrimitivesPro.GameObjects;
using System;
using System.IO;

public class Test8 : MonoBehaviour
{
    public int SESSION;

    int monitorRefreshRate = 120;

    List<Vector2> targetPositions = new List<Vector2>() { new Vector2(269.8443f, 111.7732f),
                                                          new Vector2(111.7732f, 269.8443f),
                                                          new Vector2(-111.7732f, 269.8443f),
                                                          new Vector2(-269.8443f, 111.7732f),
                                                          new Vector2(-269.8443f, -111.7732f),
                                                          new Vector2(-111.7732f, -269.8443f),
                                                          new Vector2(+111.7732f, -269.8443f),
                                                          new Vector2(269.8443f, -111.7732f)};

    List<Color> colors = new List<Color> { new Color(0.5f, 0.25f, 0.6f, 1),
                                           new Color(1, 0, 1, 1),
                                           new Color(1, 0, 0, 1),
                                           new Color(1, 0.36f, 0, 1),
                                           new Color(1, 1, 0, 1),
                                           new Color(0, 1, 0, 1),
                                           new Color(0, 1, 1, 1),
                                           new Color(0, 0, 1, 1)};

    TextMeshProUGUI FPS_text;
    TextMeshProUGUI timeText;
    TextMeshProUGUI sessionText;

    public static class Number
    {
        static public int axes = 2;
        static public int frames = 300;
        static public int trials = 480;
        static public int cursors = 3;
        static public int targetPositions = 8;
        static public int control = 2;
    }

    class Sizes
    {
        public static float target = 76.9231f;
        public static float cursor = 15;
        public static float gaze = 36.0902f;
    }

    class Labels
    {
        public static string[] control = new string[2] { "Solo", "Joint" };
        public static string[] session = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };
    }

    float[] xMod = new float[2] { -1080 / 2, +1080 / 2 };

    Material cursorMaterial;

    class Cursor
    {
        public GameObject gameObject;
        public List<Vector2> trajectory = new List<Vector2>();

        public Cursor(GameObject gameObject, List<Vector2> trajectory)
        {
            this.gameObject = gameObject;
            this.trajectory = trajectory;
        }
    }

    List<Cursor> cursors = new List<Cursor>();

    public class Ports
    {
        public static int[] P1 = new int[2] { 1, 2 };
        public static int[] P2 = new int[2] { 3, 4 };
        public static int target_positions_solo = 5;
        public static int[] P1_gaze_solo = new int[2] { 6, 7 };
        public static int[] P2_gaze_solo = new int[2] { 8, 9 };

        public static int[] joint = new int[2] { 10, 11 };
        public static int target_positions_joint = 12;
        public static int[] P1_gaze_joint = new int[2] { 13, 14 };
        public static int[] P2_gaze_joint = new int[2] { 15, 16 };

        public static int session = 17;
        public static int status = 18;
    }

    int FRAME = 0;

    public float[] timeVector = new float[Number.frames];

    bool isUseTimeVector = true;
    bool isShowFPS = true;
    bool isShowSessionNumber = false;

    bool isScreenShotMode = true;
    bool isContinuousShots = true;

    public bool isEndGame = false;

    string resultsPath;

    float[,] GetBufferData(int port)
    {
        UnityBuffer unityBuffer = new UnityBuffer();

        if (unityBuffer.connect(host: "127.0.0.1", port: port))
        {
            print("connected: " + port.ToString());
            unityBuffer.header = unityBuffer.getHeader();
            float[,] data = unityBuffer.getFloatData(0, unityBuffer.header.nSamples - 1);
            return data;
        }
        else
        {
            print("connection failed...");
            return new float[0, 0];
        }
    }

    void PutData(int port, float[,] dataToPut)
    {
        UnityBuffer unityBuffer = new UnityBuffer();

        if (unityBuffer.connect(host: "127.0.0.1", port: port))
        {
            print("connected: " + port.ToString());
            unityBuffer.putData(dataToPut);
        }
        else
        {
            print("connection failed...");
        }
    }



    float GetBufferDataStatus(int port)
    {
        UnityBuffer unityBuffer = new UnityBuffer();

        if (unityBuffer.connect(host: "127.0.0.1", port: port))
        {
            print("connected: " + port.ToString());
            unityBuffer.header = unityBuffer.getHeader();
            float[,] data = unityBuffer.getFloatData(unityBuffer.header.nSamples - 1, unityBuffer.header.nSamples - 1);
            return data[0, 0];
        }
        else
        {
            print("connection failed...");
            return 0;
        }
    }

    int PrepareSession()
    {

        float[,] statusData = GetBufferData(Ports.status);

        for (int i = 0; i < statusData.GetLength(0); i++) // not elegent, but hey
        {
            if (statusData[i, 0] == 99)
            {
                isEndGame = true;
                break;
            }
        }

        frameTimer = 0;
        FRAME = 0;

        SESSION = (int)GetBufferData(Ports.session)[0, 0];

        print("***************************************************************");
        print("SESSION: " + SESSION.ToString());

        sessionText.text = "S" + Labels.session[SESSION];

        GameObject grandParentObject = new GameObject(name: Labels.session[SESSION]);
        cursors = new List<Cursor>();

        // output setup

        if (isScreenShotMode)
        {
            string applicationPath = @Application.dataPath;
            //resultsPath = Path.GetFullPath(System.IO.Path.Combine(applicationPath, @"..\RESULTS\", Labels.session[SESSION]));
            resultsPath = Path.GetFullPath(System.IO.Path.Combine(applicationPath, @"..\RESULTS2\", Labels.session[SESSION]));

            print(resultsPath);

            //rectory.Delete(resultsPath);

            if (!Directory.Exists(resultsPath))
            {
                Directory.CreateDirectory(resultsPath);
            }
        }

        float yModToUse = 0;

        for (int CONTROL = 0; CONTROL < Number.control; CONTROL++)
        {
            DrawTargets(xMod[CONTROL], yModToUse, "targets." + Labels.control[CONTROL], grandParentObject);
        }

        float[,] target_positions_solo = GetBufferData(Ports.target_positions_solo);

        AddCursors(Ports.P1, "P1", CursorType.cursor, xMod[0], yModToUse, target_positions_solo, grandParentObject);
        AddCursors(Ports.P2, "P2", CursorType.cursor, xMod[0], yModToUse, target_positions_solo, grandParentObject);

        AddCursors(Ports.P1_gaze_solo, "P1_gaze_solo", CursorType.gaze, xMod[0], yModToUse, target_positions_solo, grandParentObject);
        AddCursors(Ports.P2_gaze_solo, "P2_gaze_solo", CursorType.gaze, xMod[0], yModToUse, target_positions_solo, grandParentObject);

        float[,] target_positions_joint = GetBufferData(Ports.target_positions_joint);

        AddCursors(Ports.joint, "joint", CursorType.cursor, xMod[1], yModToUse, target_positions_joint, grandParentObject);

        AddCursors(Ports.P1_gaze_joint, "P1_gaze_joint", CursorType.gaze, xMod[1], yModToUse, target_positions_joint, grandParentObject);
        AddCursors(Ports.P2_gaze_joint, "P2_gaze_joint", CursorType.gaze, xMod[1], yModToUse, target_positions_joint, grandParentObject);

        PutData(Ports.status, new float[1, 1] { { 0 } }); // ready for more data - length check on python side...

        return SESSION;
    }

    int currentSession;

    void AddCursors(int[] ports, string parentName, CursorType cursorType, float xMod, float yMod, float[,] target_positions, GameObject grandParent)
    {

        GameObject parent = new GameObject(name: parentName);
        parent.transform.parent = grandParent.transform;

        // get trajectories

        List<float[,]> trajectory = new List<float[,]>();

        foreach (var port in ports)
        {
            //print(port);
            trajectory.Add(GetBufferData(port));
            //printArrayDimensions(trajectory[trajectory.Count - 1]);
        }

        // build cursors

        for (int TRIAL = 0; TRIAL < Number.trials; TRIAL++)
        {
            List<Vector2> cursorTrajectory = reshapeTrajectory(trajectory, TRIAL, xMod, yMod);

            Color colorToUse = colors[(int)target_positions[TRIAL, 0]];

            float randomValue = UnityEngine.Random.Range(.5f, 1);
            colorToUse.r = colorToUse.r * randomValue;
            colorToUse.g = colorToUse.g * randomValue;
            colorToUse.b = colorToUse.b * randomValue;

            GameObject SpawnCursorObject()
            {
                switch (cursorType)
                {
                    case CursorType.cursor:
                        GameObject cursorObject1 = PlaneObject.Create(width: Sizes.cursor,
                                                          length: Sizes.cursor,
                                                          widthSegments: 1,
                                                          lengthSegments: 1).gameObject;

                        cursorObject1.GetComponent<Renderer>().material = cursorMaterial;
                        cursorObject1.GetComponent<Renderer>().material.color = colorToUse;
                        return cursorObject1;
                    case CursorType.gaze:
                        GameObject cursorObject2 = Create.Polygon(position: Vector3.zero,
                                                      rotation: new Vector3(-90, 0, 0),
                                                      radius: Sizes.gaze,
                                                      edgeThickness: .1f,
                                                      nSegments: Defaults.nSegementsCircle,
                                                      color: colorToUse,
                                                      sortingOrder: 1);
                        return cursorObject2;
                    default:
                        return new GameObject();
                }
            }

            GameObject cursorObject = SpawnCursorObject();
            cursorObject.name = "cursor." + TRIAL.ToString();
            cursorObject.transform.parent = parent.transform;

            cursors.Add(new Cursor(gameObject: cursorObject, trajectory: cursorTrajectory));
        }
    }

    void printArrayDimensions(float[,] array)
    {
        print(array.GetLength(0));
        print(array.GetLength(1));
    }

    // reshape method
    List<Vector2> reshapeTrajectory(List<float[,]> trajectoryData, int TRIAL, float xMod, float yMod)
    {
        List<Vector2> trajectoryToUse = new List<Vector2>();

        for (int FRAME = 0; FRAME < Number.frames; FRAME++)
        {
            Vector2 position = new Vector2();

            for (int AXIS = 0; AXIS < Number.axes; AXIS++)
            {
                //print(TRIAL.ToString() + ":" + FRAME.ToString() + ":" + AXIS.ToString());
                if (AXIS == 0)
                    position[AXIS] = trajectoryData[AXIS][TRIAL, FRAME] + xMod;
                else
                    position[AXIS] = trajectoryData[AXIS][TRIAL, FRAME] + yMod;
            }
            trajectoryToUse.Add(position);
        }

        return trajectoryToUse;
    }

    // add target positions

    void DrawTargets(float xMod, float yMod, string parentName, GameObject grandParent)
    {
        GameObject targets = new GameObject(name: parentName);
        targets.transform.parent = grandParent.transform;

        for (int TARGET = 0; TARGET < Number.targetPositions; TARGET++)
        {
            GameObject target = Create.Polygon(position: new Vector3(targetPositions[TARGET].x + xMod, 0, targetPositions[TARGET].y + yMod),
                                               rotation: new Vector3(-90, 0, 0),
                                               radius: Sizes.target,
                                               edgeThickness: .1f,
                                               nSegments: Defaults.nSegementsCircle,
                                               color: Color.white, //colors[TARGET],
                                               sortingOrder: 2);

            target.name = "target." + TARGET.ToString();
            target.transform.parent = targets.transform;
        }
    }

    void StartNewSession()
    {
        currentSession = PrepareSession();
    }

    void Start()
    {
        UnityEngine.Random.InitState(255);

        // stopwatch

        Stopwatch sw = new Stopwatch();
        sw.Start();

        // mode setup

        if (isScreenShotMode)
        {
            isUseTimeVector = false;
            isShowFPS = false;
        }

        // timeVector setup

        for (int FRAME = 0; FRAME < Number.frames; FRAME++)
        {
            timeVector[FRAME] = (float)FRAME / monitorRefreshRate;
        }

        // scene setup

        FPS_text = GameObject.Find("Canvas/FPS").GetComponent<TextMeshProUGUI>();
        cursorMaterial = Resources.Load("UnlitColor") as Material;
        timeText = GameObject.Find("Canvas/Time").GetComponent<TextMeshProUGUI>();
        sessionText = GameObject.Find("Canvas/Session").GetComponent<TextMeshProUGUI>();

        if (!isShowFPS)
            FPS_text.enabled = false;

        if (!isShowSessionNumber)
            sessionText.enabled = false;

        StartNewSession();
        print(sw.Elapsed.Seconds);

    }



    enum CursorType
    {
        cursor,
        gaze
    }

    float frameTimer = 0;

    string formatTimeString(float nearestTime)
    {
        string timeString = String.Format("{0:0.##}", nearestTime);

        if (timeString.Length < 4) // probably not nececessary but haven't checked
            timeString += "0";

        if (timeString == "00")
            timeString = "0.00";

        if (timeString == "10")
            timeString = "1.00";

        if (timeString == "20")
            timeString = "2.00";

        return timeString;
    }

    void Update()
    {

        // determine FRAME & time

        if (isUseTimeVector) // frame rate independent plotting
        {
            frameTimer += Time.unscaledDeltaTime;

            if (frameTimer > timeVector.Max())
                frameTimer = 0;

            float nearestTime = timeVector.Select(p => new { Value = p, Difference = Math.Abs(p - frameTimer) })
                  .OrderBy(p => p.Difference)
                  .First().Value;

            FRAME = Array.IndexOf(timeVector, nearestTime);
            timeText.text = "Time: " + formatTimeString(nearestTime) + " s";
            //print(frameTimer.ToString() + ": " + nearestTime.ToString() + ": " + FRAME.ToString());
        }
        else // frame rate dependent plotting
        {
            if (FRAME == Number.frames)
            {
                if (isScreenShotMode)
                {
                    if (isContinuousShots)
                    {
                        if (isEndGame)
                        {
                            QuitGame();
                        }

                        Destroy(GameObject.Find(Labels.session[currentSession]));
                        currentSession = PrepareSession();

                    }
                    else
                    {
                        QuitGame();
                    }
                }
                else
                {
                    FRAME = 0;
                }

            }
            timeText.text = "Time: " + formatTimeString(timeVector[FRAME]) + " s";
        }

        // cursors

        foreach (var cursor in cursors)
        {
            if (!float.IsNaN(cursor.trajectory[FRAME].x) && !float.IsNaN(cursor.trajectory[FRAME].y))
            {
                cursor.gameObject.SetActive(true);
                cursor.gameObject.transform.position = new Vector3(cursor.trajectory[FRAME].x, 0, cursor.trajectory[FRAME].y);
            }
            else
            {
                cursor.gameObject.SetActive(false);
            }
        }

        // FPS

        if (Time.unscaledTime > FPS_timer) // update
        {
            int fps = (int)(FPS_list.Count > 0 ? FPS_list.Average() : 0.0); // average frame rate over last second
            FPS_text.text = "FPS: " + fps;
            FPS_timer = Time.unscaledTime + FPS_refreshRate;
            FPS_list.Clear();
        }
        else
        {
            int fps = (int)(1f / Time.unscaledDeltaTime);
            FPS_list.Add(fps);
        }

        // screenshot

        if (isScreenShotMode)
        {
            string frameString = FRAME.ToString();

            if (FRAME < 10)
            {
                frameString = "00" + frameString;
            }
            else if (FRAME < 100)
            {
                frameString = "0" + frameString;
            }

            ScreenCapture.CaptureScreenshot(filename: resultsPath + "//" + frameString + ".png", superSize: 1);

        }

        if (!isUseTimeVector)
        {
            FRAME++;
        }

    }

    float FPS_refreshRate = 1f; // seconds
    float FPS_timer = 1f; // seconds
    List<int> FPS_list = new List<int>();

    public void QuitGame()
    {
        // save any game data here
#if UNITY_EDITOR
        // Application.Quit() does not work in the editor so
        // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
        UnityEditor.EditorApplication.isPlaying = false;
#else
                 Application.Quit();
#endif
    }

}



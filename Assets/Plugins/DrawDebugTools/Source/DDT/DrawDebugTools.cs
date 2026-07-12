using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Numerics;
using UnityEngine.AI;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

[DefaultExecutionOrder(100)]
public class DrawDebugTools : MonoBehaviour
{
    #region ========== Variables ==========

    private static DrawDebugTools instance;

    [Header("Settings")] [Tooltip("Settings file used to change some parameters")]
    public DDTSettings m_DDTSettings;

    // Lines
    private List<BatchedLine> m_BatchedLines;
    private Mesh m_Mesh;

    // Quads
    private List<DebugBillboard> m_DebugBillboardsList;
    private MaterialPropertyBlock m_QuadMatPropertyBlock;
    
    // Materials
    private Material m_LineMaterial;
    private Material m_QuadMaterial;

    // Text
    private List<DebugText> m_DebugTextesList;
    private GameObject m_3DTextesParent;
    private GameObject m_3DTextePrefab;
    private List<TextMesh> m_3DTextesList;

    // Debug camera
    private GameObject m_DebugCameraPrefab;
    private DDTCamera m_DebugCamera;
    private List<Camera> m_GameCamerasList;
    private bool m_IsCursorVisible = false;
    private CursorLockMode m_CursorLockMode;

    private GameObject m_MainCamera = null;
    private bool m_DebugCameraIsActive = false;

    // Debug float 
    private List<DebugFloatGraph> m_FloatGraphsList;
    private int m_FloatGraphSamplesCount = 20;

    // Log message
    private List<DebugLogMessage> m_LogMessagesList;
    GameObject m_DDTCanvasPrefab;
    DDTCanvas m_DDTCanvas;

    #region ========== Properties ==========

    public static DrawDebugTools Instance
    {
        get
        {
            if (instance == null)
            {
                instance = GameObject.FindObjectOfType<DrawDebugTools>();
                if (instance == null)
                    Debug.LogError(
                        "Please place the prefab [DrawDebugTools] in the scene before calling drawing functions !");
            }

            return instance;
        }

        set { instance = value; }
    }

    public DDTCamera DebugCamera
    {
        get { return m_DebugCamera; }

        set { m_DebugCamera = value; }
    }

    public GameObject MainCamera
    {
        get { return m_MainCamera; }

        set { m_MainCamera = value; }
    }

    public bool DebugCameraIsActive
    {
        get { return m_DebugCameraIsActive; }

        set { m_DebugCameraIsActive = value; }
    }

    #endregion

    #endregion

    #region ========== Initialization ==========

    private void Awake()
    {
        // Make sure there no other ddt object in the scene
        DrawDebugTools[] DDTsArray = GameObject.FindObjectsOfType<DrawDebugTools>();
        if (DDTsArray.Length > 1) Destroy(gameObject);

        // Set instance
        instance = this;

        // Keep it when we switch scenes
        DontDestroyOnLoad(gameObject);

        // Init batched lines
        m_BatchedLines = new List<BatchedLine>();
        m_Mesh = new Mesh();

        // Init debug text list
        m_DebugTextesList = new List<DebugText>();
        m_3DTextesList = new List<TextMesh>();
        m_3DTextePrefab = Resources.Load<GameObject>("Prefabs/DDT3DText");

        // Float graphs
        m_FloatGraphsList = new List<DebugFloatGraph>();

        // Debug quads
        m_DebugBillboardsList = new List<DebugBillboard>();
        m_QuadMatPropertyBlock = new MaterialPropertyBlock();

        // Debug camera
        m_DebugCameraPrefab = Resources.Load<GameObject>("Prefabs/DDTDebugCamera");
        m_GameCamerasList = new List<Camera>();

        // Init log messsages list
        m_LogMessagesList = new List<DebugLogMessage>();
        m_DDTCanvasPrefab = Resources.Load<GameObject>("Prefabs/DDTCanvas");

        // Make sure settings params are valid
        if (m_DDTSettings == null)
        {
            m_DDTSettings = new DDTSettings();
        }
    }

    private void Start()
    {
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        if (!m_LineMaterial)
        {
            Shader Shader = Shader.Find("Hidden/Internal-Colored");
            if (Shader == null)
            {
                Debug.LogWarning("DrawDebugTools: 未找到 Hidden/Internal-Colored，跳过线框材质初始化。");
                return;
            }

            m_LineMaterial = new Material(Shader);
            m_LineMaterial.hideFlags = HideFlags.HideAndDontSave;

            //Turn on alpha blending
            m_LineMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_LineMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m_LineMaterial.SetOverrideTag("ZWrite", "On");
            m_LineMaterial.SetOverrideTag("ZTest", "LEqual");

            //Turn backface culling off
            m_LineMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        }

        if (!m_QuadMaterial)
        {
            Shader Shader = Shader.Find("Unlit/Transparent");
            if (Shader == null)
            {
                Debug.LogWarning("DrawDebugTools: 未找到 Unlit/Transparent，跳过 Quad 材质初始化。");
                return;
            }

            m_QuadMaterial = new Material(Shader);
            m_QuadMaterial.hideFlags = HideFlags.HideAndDontSave;

            // //Turn on alpha blending
            m_QuadMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_QuadMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m_QuadMaterial.SetOverrideTag("ZWrite", "Off");
            m_QuadMaterial.SetOverrideTag("ZTest", "LEqual");

            // //Turn backface culling off
            m_QuadMaterial.SetInt("_Cull", (int) UnityEngine.Rendering.CullMode.Off);
        }
    }

    #endregion

    #region ========== Update Function ==========

    private void Update()
    {
        // Reset pos and rot
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Handle different elements drawing
        HandleDebugCamera();
        HandleDrawingListOfLines();
        HandleDrawingListOfBillboards();
        HandleDrawingListOfTextes();
        HandleListOfLogMessagesList();
        HandleDrawingListOfFloatGraphs();
    }

    #endregion

    #region ========== Camera Debug ==========

    private void HandleDebugCamera()
    {
        // Toggle debug camera 
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ToggleDebugCamera();
        }

        if (m_DebugCameraIsActive)
        {
            // Debug camera raycast
            RaycastHit DebugHitInfos;
            if (Physics.Raycast(m_DebugCamera.transform.position, m_DebugCamera.transform.forward, out DebugHitInfos,
                1000.0f))
            {
                // Draw normal line
                DrawLine(DebugHitInfos.point, DebugHitInfos.point + DebugHitInfos.normal,
                    m_DDTSettings.m_DebugNormalVectColor);
            }

            // Draw other cameras
            foreach (var CamItem in m_GameCamerasList)
            {
                DrawCamera(CamItem, m_DDTSettings.m_MainCamShapeColor);
            }

            // Update debug camera ui infos
            m_DDTCanvas.UpdateDebugCamera(DebugHitInfos);
        }
    }

    public void ToggleDebugCamera()
    {
        // Set cameras list
        m_GameCamerasList.Clear();
        m_GameCamerasList = GameObject.FindObjectsOfType<Camera>().ToList<Camera>();

        if (m_DebugCameraIsActive)
        {
            // Delete debug camera 
            m_DebugCamera.gameObject.SetActive(false);
            m_DebugCameraIsActive = false;

            // Deactivate game debug infos ui
            if (m_DDTCanvas && m_DDTCanvas.DebugCameraInfos)
                m_DDTCanvas.DebugCameraInfos.gameObject.SetActive(false);

            // Cursor state
            Cursor.visible = m_IsCursorVisible;
            Cursor.lockState = m_CursorLockMode;
        }
        else
        {
            // Create debug camera if doesn't exist
            if (m_DebugCamera == null)
            {
                m_DebugCamera = GameObject.Instantiate(m_DebugCameraPrefab).GetComponent<DDTCamera>();
                m_DebugCamera.transform.SetParent(transform);
            }
            else
            {
                // Activate debug camera
                m_DebugCamera.gameObject.SetActive(true);
            }

            // get curren main camera
            m_MainCamera = Camera.main.gameObject;

            // Set debug camera
            if (m_MainCamera)
            {
                // Set pos / rot
                m_DebugCamera.transform.position = m_MainCamera.transform.position;
                m_DebugCamera.transform.rotation = m_MainCamera.transform.rotation;
                // Set cam flag
                m_DebugCamera.GetComponent<Camera>().clearFlags = m_MainCamera.GetComponent<Camera>().clearFlags;
                m_DebugCamera.GetComponent<Camera>().backgroundColor =
                    m_MainCamera.GetComponent<Camera>().backgroundColor;
                // Set fov
                m_DebugCamera.GetComponent<Camera>().fieldOfView = m_MainCamera.GetComponent<Camera>().fieldOfView;
                // Set rot variables
                m_DebugCamera.InitializeCamRotation();
            }
            else
            {
                Debug.LogError("Main camera object doesn't exist!");
            }

            // Cursor state
            m_IsCursorVisible = Cursor.visible;
            m_CursorLockMode = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;


            // Set debug camera active flag
            m_DebugCameraIsActive = true;

            // Set time scale to 0
            if (m_DDTSettings.m_IsDebugCamFreezeTime)
            {
                Time.timeScale = 0.0f;
            }
        }
    }

    #endregion

    #region ========== Drawing Functions ==========

    /// <summary>
    /// Draw a wire sphere
    /// </summary>
    /// <param name="Center">Position of the sphere</param>
    /// <param name="Radius">Raduis of the sphere</param>
    /// <param name="Segments">Segments count that form the sphere</param>
    /// <param name="Color">Color of the sphere</param>
    /// <param name="LifeTime">Lifetime before stop drawing the sphere</param>
    public static void DrawSphere(Vector3 Center, float Radius, int Segments, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        DrawSphere(Center, Quaternion.identity, Radius, Segments, Color, LifeTime);
    }

    /// <summary>
    /// Method to draw a wire sphere
    /// </summary>
    /// <param name="Center">Position of the sphere</param>
    /// <param name="Rotation"></param>
    /// <param name="Radius">Raduis of the sphere</param>
    /// <param name="Segments">Segments count that form the sphere</param>
    /// <param name="Color">Color of the sphere</param>
    /// <param name="LifeTime">Lifetime before stop drawing the sphere</param>
    public static void DrawSphere(Vector3 Center, Quaternion Rotation, float Radius, int Segments, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        Segments = Mathf.Max(Segments, 4);
        Segments = (int) Mathf.Round((float) Segments / 4.0f) * 4;

        float AngleInc = 2.0f * Mathf.PI / (float) Segments;

        List<BatchedLine> Lines;
        Lines = new List<BatchedLine>();

        for (int i = 0; i < Segments; i++)
        {
            float PolarAngle = AngleInc;
            float AzimuthalAngle = AngleInc * i;

            float Point_1_X = Mathf.Sin(PolarAngle) * Mathf.Cos(AzimuthalAngle);
            float Point_1_Y = Mathf.Cos(PolarAngle);
            float Point_1_Z = Mathf.Sin(PolarAngle) * Mathf.Sin(AzimuthalAngle);

            float Point_2_X;
            float Point_2_Y;
            float Point_2_Z;

            for (int J = 0; J < Segments; J++)
            {

                Point_2_X = Mathf.Sin(PolarAngle) * Mathf.Cos(AzimuthalAngle);
                Point_2_Y = Mathf.Cos(PolarAngle);
                Point_2_Z = Mathf.Sin(PolarAngle) * Mathf.Sin(AzimuthalAngle);

                float Point_3_X = Mathf.Sin(PolarAngle) * Mathf.Cos(AzimuthalAngle + AngleInc);
                float Point_3_Y = Mathf.Cos(PolarAngle);
                float Point_3_Z = Mathf.Sin(PolarAngle) * Mathf.Sin(AzimuthalAngle + AngleInc);

                Vector3 Point_1 = new Vector3(Point_1_X, Point_1_Y, Point_1_Z) * Radius + Center;
                Vector3 Point_2 = new Vector3(Point_2_X, Point_2_Y, Point_2_Z) * Radius + Center;
                Vector3 Point_3 = new Vector3(Point_3_X, Point_3_Y, Point_3_Z) * Radius + Center;

                Lines.Add(new BatchedLine(Point_1, Point_2, Center, Rotation, Color, LifeTime));
                Lines.Add(new BatchedLine(Point_2, Point_3, Center, Rotation, Color, LifeTime));

                Point_1_X = Point_2_X;
                Point_1_Y = Point_2_Y;
                Point_1_Z = Point_2_Z;

                PolarAngle += AngleInc;
            }
        }
        
        DrawDebugTools.Instance.AddRangeLine(Lines);
    }

    /// <summary>
    /// Draw a 3D line in space
    /// </summary>
    /// <param name="LineStart">Position of the line start</param>
    /// <param name="LineEnd">Position of the line end</param>
    /// <param name="Color">Color of the line</param>
    /// <param name="LifeTime">Line life time</param>
    public static void DrawLine(Vector3 LineStart, Vector3 LineEnd, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        DrawDebugTools.Instance.AddLine(new BatchedLine(LineStart, LineEnd, Vector3.zero,
            Quaternion.identity, Color, LifeTime));
    }

    /// <summary>
    /// Draw a 3D point in space
    /// </summary>
    /// <param name="Position">Position of the point</param>
    /// <param name="Size">Size of the point</param>
    /// <param name="Color">Color of the point</param>
    /// <param name="LifeTime">Point life time</param>
    public static void DrawPoint(Vector3 Position, float Size, Color Color, float LifeTime = 0.0f)
    {
        // X
        InternalDrawLine(Position + new Vector3(-Size / 2.0f, 0.0f, 0.0f),
            Position + new Vector3(Size / 2.0f, 0.0f, 0.0f), Position, Quaternion.identity, Color, LifeTime);
        // Y
        InternalDrawLine(Position + new Vector3(0.0f, -Size / 2.0f, 0.0f),
            Position + new Vector3(0.0f, Size / 2.0f, 0.0f), Position, Quaternion.identity, Color, LifeTime);
        // Z
        InternalDrawLine(Position + new Vector3(0.0f, 0.0f, -Size / 2.0f),
            Position + new Vector3(0.0f, 0.0f, Size / 2.0f), Position, Quaternion.identity, Color, LifeTime);
    }

    /// <summary>
    /// Draw directional arrow
    /// </summary>
    /// <param name="ArrowStart">Arrow start position</param>
    /// <param name="ArrowEnd">Arrow end position</param>
    /// <param name="ArrowSize">Arrow size</param>
    /// <param name="Color">Arrow color</param>
    /// <param name="LifeTime">Arrow life time</param>
    public static void DrawDirectionalArrow(Vector3 ArrowStart, Vector3 ArrowEnd, float ArrowSize, Color Color,
        float LifeTime = 0.0f)
    {
        InternalDrawLine(ArrowStart, ArrowEnd, ArrowStart, Quaternion.identity, Color, LifeTime);

        Vector3 Dir = (ArrowEnd - ArrowStart).normalized;
        Vector3 Right = Vector3.Cross(Vector3.up, Dir);

        InternalDrawLine(ArrowEnd, ArrowEnd + (Right - Dir.normalized) * ArrowSize, ArrowStart, Quaternion.identity,
            Color, LifeTime);
        InternalDrawLine(ArrowEnd, ArrowEnd + (-Right - Dir.normalized) * ArrowSize, ArrowStart, Quaternion.identity,
            Color, LifeTime);
    }

    /// <summary>
    /// Draw a box
    /// </summary>
    /// <param name="Center">Center position of the box</param>
    /// <param name="Rotation">Rotaion of the box</param>
    /// <param name="Size">The size of the box</param>
    /// <param name="Color">Color of the box</param>
    /// <param name="LifeTime">Box life time</param>
    public static void DrawBox(Vector3 Center, Quaternion Rotation, Vector3 Size, Color Color, float LifeTime = 0.0f)
    {
        InternalDrawLine(Center + new Vector3(Size.x, Size.y, Size.z) / 2.0f,
            Center + new Vector3(Size.x, -Size.y, Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(Size.x, -Size.y, Size.z) / 2.0f,
            Center + new Vector3(-Size.x, -Size.y, Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Size.x, -Size.y, Size.z) / 2.0f,
            Center + new Vector3(-Size.x, Size.y, Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Size.x, Size.y, Size.z) / 2.0f,
            Center + new Vector3(Size.x, Size.y, Size.z) / 2.0f, Center, Rotation, Color, LifeTime);

        InternalDrawLine(Center + new Vector3(Size.x, Size.y, -Size.z) / 2.0f,
            Center + new Vector3(Size.x, -Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(Size.x, -Size.y, -Size.z) / 2.0f,
            Center + new Vector3(-Size.x, -Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Size.x, -Size.y, -Size.z) / 2.0f,
            Center + new Vector3(-Size.x, Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Size.x, Size.y, -Size.z) / 2.0f,
            Center + new Vector3(Size.x, Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);

        InternalDrawLine(Center + new Vector3(Size.x, Size.y, Size.z) / 2.0f,
            Center + new Vector3(Size.x, Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(Size.x, -Size.y, Size.z) / 2.0f,
            Center + new Vector3(Size.x, -Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Size.x, -Size.y, Size.z) / 2.0f,
            Center + new Vector3(-Size.x, -Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Size.x, Size.y, Size.z) / 2.0f,
            Center + new Vector3(-Size.x, Size.y, -Size.z) / 2.0f, Center, Rotation, Color, LifeTime);
    }

    /// <summary>
    /// Draw a 3D circle
    /// </summary>
    /// <param name="Center">Centre position of the circle</param>
    /// <param name="Rotation">Rotation of the circle</param>
    /// <param name="Radius">Radius of the circle</param>
    /// <param name="Segments">Segments count in the circle</param>
    /// <param name="Color">Color of the circle</param>
    /// <param name="LifeTime">Circle life time</param>
    public static void DrawCircle(Vector3 Center, Quaternion Rotation, float Radius, int Segments, Color Color,
        float LifeTime = 0.0f)
    {
        Segments = Mathf.Max(Segments, 4);
        Segments = (int) Mathf.Round((float) Segments / 4.0f) * 4;

        float AngleInc = 2.0f * Mathf.PI / (float) Segments;

        float Angle = 0.0f;
        for (int i = 0; i < Segments; i++)
        {
            Vector3 Point_1 = Center + Radius * new Vector3(Mathf.Cos(Angle), 0.0f, Mathf.Sin(Angle));
            Angle += AngleInc;
            Vector3 Point_2 = Center + Radius * new Vector3(Mathf.Cos(Angle), 0.0f, Mathf.Sin(Angle));
            InternalDrawLine(Point_1, Point_2, Center, Rotation, Color, LifeTime);
        }
    }

    /// <summary>
    /// Draw a 3D circle on a plane defined by axis (XZ, XY, YZ)
    /// </summary>
    /// <param name="Center">Centre position of the circle</param>
    /// <param name="Radius">Radius of the circle</param>
    /// <param name="Segments">Segments count in the circle</param>
    /// <param name="Color">Color of the circle</param>
    /// <param name="DrawPlaneAxis">Plane axis to draw circle in (XZ, XY, YZ)</param>
    /// <param name="LifeTime">Circle life time</param>
    public static void DrawCircle(Vector3 Center, float Radius, int Segments, Color Color,
        EDrawPlaneAxis DrawPlaneAxis = EDrawPlaneAxis.XZ, float LifeTime = 0.0f)
    {
        Segments = Mathf.Max(Segments, 4);
        Segments = (int) Mathf.Round((float) Segments / 4.0f) * 4;

        float AngleInc = 2.0f * Mathf.PI / (float) Segments;

        float Angle = 0.0f;
        switch (DrawPlaneAxis)
        {
            case EDrawPlaneAxis.XZ:
                for (int i = 0; i < Segments; i++)
                {
                    Vector3 Point_1 = Center + Radius * new Vector3(Mathf.Cos(Angle), 0.0f, Mathf.Sin(Angle));
                    Angle += AngleInc;
                    Vector3 Point_2 = Center + Radius * new Vector3(Mathf.Cos(Angle), 0.0f, Mathf.Sin(Angle));
                    InternalDrawLine(Point_1, Point_2, Center, Quaternion.identity, Color, LifeTime);
                }

                break;
            case EDrawPlaneAxis.XY:
                for (int i = 0; i < Segments; i++)
                {
                    Vector3 Point_1 = Center + Radius * new Vector3(0.0f, Mathf.Sin(Angle), Mathf.Cos(Angle));
                    Angle += AngleInc;
                    Vector3 Point_2 = Center + Radius * new Vector3(0.0f, Mathf.Sin(Angle), Mathf.Cos(Angle));
                    InternalDrawLine(Point_1, Point_2, Center, Quaternion.identity, Color, LifeTime);
                }

                break;
            case EDrawPlaneAxis.YZ:
                for (int i = 0; i < Segments; i++)
                {
                    Vector3 Point_1 = Center + Radius * new Vector3(Mathf.Cos(Angle), Mathf.Sin(Angle));
                    Angle += AngleInc;
                    Vector3 Point_2 = Center + Radius * new Vector3(Mathf.Cos(Angle), Mathf.Sin(Angle));
                    InternalDrawLine(Point_1, Point_2, Center, Quaternion.identity, Color, LifeTime);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Draw a 3D coordinates
    /// </summary>
    /// <param name="Position">Position of the coordinates</param>
    /// <param name="Rotation">Rotation of the coordinates</param>
    /// <param name="Scale">Scale of the coordinate</param>
    /// <param name="LifeTime">Coordinates lifetime</param>
    public static void Draw3DCoordinates(Vector3 Position, Quaternion Rotation, float Scale, float LifeTime = 0.0f)
    {
        InternalDrawLine(Position, Position + new Vector3(Scale, 0.0f, 0.0f), Position, Rotation, Color.red, LifeTime);
        InternalDrawLine(Position, Position + new Vector3(0.0f, Scale, 0.0f), Position, Rotation, Color.green,
            LifeTime);
        InternalDrawLine(Position, Position + new Vector3(0.0f, 0.0f, Scale), Position, Rotation, Color.blue, LifeTime);
    }

    /// <summary>
    /// Draw a 3D cylinder
    /// </summary>
    /// <param name="Start">Cylinder start position</param>
    /// <param name="End">Cylinder end position</param>
    /// <param name="Radius">Cylinder radius</param>
    /// <param name="Segments">Cylinder segments count</param>
    /// <param name="Color">Color of the cylinder</param>
    /// <param name="LifeTime">Cylinder life time</param>
    public static void DrawCylinder(Vector3 Start, Vector3 End, float Radius, int Segments, Color Color,
        float LifeTime = 0.0f)
    {
        Vector3 Center = (Start + End) / 2.0f;
        InternalDrawCylinder(Start, End, Quaternion.identity, Center, Radius, Segments, Color, LifeTime);
    }

    /// <summary>
    /// Draw a 3D cone
    /// </summary>
    /// <param name="Position">Cone position</param>
    /// <param name="Direction">Cone direction</param>
    /// <param name="Length">Cone length</param>
    /// <param name="AngleWidth">Cone angle with</param>
    /// <param name="AngleHeight">Cone angle height</param>
    /// <param name="Segments">Cone segments count</param>
    /// <param name="Color">Cone color</param>
    /// <param name="LifeTime">Cone life time</param>
    public static void DrawCone(Vector3 Position, Vector3 Direction, float Length, float AngleWidth, float AngleHeight,
        int Segments, Color Color, float LifeTime = 0.0f)
    {
        Segments = Mathf.Max(Segments, 4);

        float SmallNumber = 0.001f;
        float Angle1 = Mathf.Clamp(AngleHeight * Mathf.Deg2Rad, SmallNumber, Mathf.PI - SmallNumber);
        float Angle2 = Mathf.Clamp(AngleWidth * Mathf.Deg2Rad, SmallNumber, Mathf.PI - SmallNumber);

        float SinX2 = Mathf.Sin(0.5f * Angle1);
        float SinY2 = Mathf.Sin(0.5f * Angle2);

        float SqrSinX2 = SinX2 * SinX2;
        float SqrSinY2 = SinY2 * SinY2;

        float TanX2 = Mathf.Tan(0.5f * Angle1);
        float TanY2 = Mathf.Tan(0.5f * Angle2);

        Vector3[] ConeVerts;
        ConeVerts = new Vector3[Segments];

        for (int i = 0; i < Segments; i++)
        {
            float AngleFragment = (float) i / (float) (Segments);
            float ThiAngle = 2.0f * Mathf.PI * AngleFragment;
            float PhiAngle = Mathf.Atan2(Mathf.Sin(ThiAngle) * SinY2, Mathf.Cos(ThiAngle) * SinX2);
            float SinPhiAngle = Mathf.Sin(PhiAngle);
            float CosPhiAngle = Mathf.Cos(PhiAngle);
            float SqrSinPhi = SinPhiAngle * SinPhiAngle;
            float SqrCosPhi = CosPhiAngle * CosPhiAngle;

            float RSq = SqrSinX2 * SqrSinY2 / (SqrSinX2 * SqrSinPhi + SqrSinY2 * SqrCosPhi);
            float R = Mathf.Sqrt(RSq);
            float Sqr = Mathf.Sqrt(1 - RSq);
            float Alpha = R * CosPhiAngle;
            float Beta = R * SinPhiAngle;


            ConeVerts[i].x = (1 - 2 * RSq);
            ConeVerts[i].y = 2 * Sqr * Alpha;
            ConeVerts[i].z = 2 * Sqr * Beta;
        }

        Vector3 ConeDirection = Direction.normalized;

        Vector3 AngleFromDirection = Quaternion.LookRotation(ConeDirection, Vector3.up).eulerAngles -
                                     new Vector3(0.0f, 90.0f, 0.0f);
        Quaternion Q = Quaternion.Euler(new Vector3(AngleFromDirection.z, AngleFromDirection.y, -AngleFromDirection.x));
        Matrix4x4 M = Matrix4x4.TRS(Position, Q, Vector3.one * Length);

        Vector3 CurrentPoint = Vector3.zero;
        Vector3 PrevPoint = Vector3.zero;
        Vector3 FirstPoint = Vector3.zero;

        for (int i = 0; i < Segments; i++)
        {
            CurrentPoint = M.MultiplyPoint(ConeVerts[i]);
            DrawLine(Position, CurrentPoint, Color, LifeTime);

            if (i == 0)
            {
                FirstPoint = CurrentPoint;
            }
            else
            {
                DrawLine(PrevPoint, CurrentPoint, Color, LifeTime);
            }

            PrevPoint = CurrentPoint;
        }

        DrawLine(CurrentPoint, FirstPoint, Color, LifeTime);
    }

    /// <summary>
    /// Draw 3D text
    /// </summary>
    /// <param name="Position">Position of the text</param>
    /// <param name="Rotation">Rotation of the text</param>
    /// <param name="Text">Text string</param>
    /// <param name="Anchor">Text anchor</param>
    /// <param name="TextColor">Text color</param>
    /// <param name="TextSize">Text size</param>
    /// <param name="LifeTime">Text life time</param>
    public static void DrawString3D(Vector3 Position, Quaternion Rotation, string Text, TextAnchor Anchor,
        Color TextColor, float TextSize = 1.0f, float LifeTime = 0.0f)
    {
        InternalAddDebugText(Text, Anchor, Position, Rotation, TextColor, TextSize, LifeTime);
    }

    /// <summary>
    /// Draw 3D text towards camera
    /// </summary>
    /// <param name="Position">Position of the text</param>
    /// <param name="Text">Text string</param>
    /// <param name="Anchor">Text anchor</param>
    /// <param name="TextColor">Text color</param>
    /// <param name="TextSize">Text size</param>
    /// <param name="LifeTime">Text life time</param>
    public static void DrawString3D(Vector3 Position, string Text, TextAnchor Anchor, Color TextColor,
        float TextSize = 1.0f, float LifeTime = 0.0f)
    {
        InternalAddDebugText(Text, Anchor, Position,
            Quaternion.LookRotation((Camera.main.transform.position - Position).normalized), TextColor, TextSize,
            LifeTime);
    }

    /// <summary>
    /// Draw camera frustum
    /// </summary>
    /// <param name="Camera">Target camera</param>
    /// <param name="Color">Frustum color</param>
    /// <param name="LifeTime">Frustum life time</param>
    public static void DrawFrustum(Camera Camera, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        Plane[] FrustumPlanes = DrawDebugTools.Instance.DebugCameraIsActive
            ? GeometryUtility.CalculateFrustumPlanes(DrawDebugTools.Instance.MainCamera.GetComponent<Camera>())
            : GeometryUtility.CalculateFrustumPlanes(Camera);
        Vector3[] NearPlaneCorners = new Vector3[4];
        Vector3[] FarePlaneCorners = new Vector3[4];

        Plane TempPlane = FrustumPlanes[1];
        FrustumPlanes[1] = FrustumPlanes[2];
        FrustumPlanes[2] = TempPlane;

        for (int i = 0; i < 4; i++)
        {
            NearPlaneCorners[i] =
                DrawDebugTools.Instance.GetIntersectionPointOfPlanes(FrustumPlanes[4], FrustumPlanes[i],
                    FrustumPlanes[(i + 1) % 4]);
            FarePlaneCorners[i] =
                DrawDebugTools.Instance.GetIntersectionPointOfPlanes(FrustumPlanes[5], FrustumPlanes[i],
                    FrustumPlanes[(i + 1) % 4]);
        }

        for (int i = 0; i < 4; i++)
        {
            InternalDrawLine(NearPlaneCorners[i], NearPlaneCorners[(i + 1) % 4], Vector3.zero, Quaternion.identity,
                Color, LifeTime);
            InternalDrawLine(FarePlaneCorners[i], FarePlaneCorners[(i + 1) % 4], Vector3.zero, Quaternion.identity,
                Color, LifeTime);
            InternalDrawLine(NearPlaneCorners[i], FarePlaneCorners[i], Vector3.zero, Quaternion.identity, Color,
                LifeTime);
        }
    }

    /// <summary>
    /// Draw 3D capsule
    /// </summary>
    /// <param name="Center">Center position of the capsule</param>
    /// <param name="HalfHeight">Capsule half height</param>
    /// <param name="Radius">Capsule radius</param>
    /// <param name="Rotation">Capsule rotation</param>
    /// <param name="Color">Capsule color</param>
    /// <param name="LifeTime">Capsule life time</param>
    public static void DrawCapsule(Vector3 Center, float HalfHeight, float Radius, Quaternion Rotation, Color Color,
        float LifeTime = 0.0f)
    {
        int Segments = 16;

        Matrix4x4 M = Matrix4x4.TRS(Vector3.zero, Rotation.normalized, Vector3.one);

        Vector3 AxisX = M.MultiplyVector(Vector3.right);
        Vector3 AxisY = M.MultiplyVector(Vector3.up);
        Vector3 AxisZ = M.MultiplyVector(Vector3.forward);

        float HalfMaxed = Mathf.Max(HalfHeight - Radius, 0.1f);
        Vector3 TopPoint = Center + HalfMaxed * AxisY;
        Vector3 BottomPoint = Center - HalfMaxed * AxisY;

        InternalDrawCapsuleCircle(TopPoint, AxisX, AxisZ, Color, Radius, Segments, LifeTime);
        InternalDrawCapsuleCircle(BottomPoint, AxisX, AxisZ, Color, Radius, Segments, LifeTime);

        InternalDrawHalfCircle(TopPoint, AxisX, AxisY, Color, Radius, Segments, LifeTime);
        InternalDrawHalfCircle(TopPoint, AxisZ, AxisY, Color, Radius, Segments, LifeTime);

        InternalDrawHalfCircle(BottomPoint, AxisX, -AxisY, Color, Radius, Segments, LifeTime);
        InternalDrawHalfCircle(BottomPoint, AxisZ, -AxisY, Color, Radius, Segments, LifeTime);

        InternalDrawLine(TopPoint + Radius * AxisX, BottomPoint + Radius * AxisX, Vector3.zero, Quaternion.identity,
            Color, LifeTime);
        InternalDrawLine(TopPoint - Radius * AxisX, BottomPoint - Radius * AxisX, Vector3.zero, Quaternion.identity,
            Color, LifeTime);
        InternalDrawLine(TopPoint + Radius * AxisZ, BottomPoint + Radius * AxisZ, Vector3.zero, Quaternion.identity,
            Color, LifeTime);
        InternalDrawLine(TopPoint - Radius * AxisZ, BottomPoint - Radius * AxisZ, Vector3.zero, Quaternion.identity,
            Color, LifeTime);
    }

    /// <summary>
    /// Draw a 3D representation of the main camera
    /// </summary>
    /// <param name="Color">Camera shape color</param>
    /// <param name="Scale">Scale of the shape</param>
    /// <param name="LifeTime">Shape life time</param>
    public static void DrawActiveCamera(Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        Camera ActiveCam = Camera.main;
        if (DrawDebugTools.Instance.DebugCameraIsActive)
            ActiveCam = DrawDebugTools.Instance.MainCamera.GetComponent<Camera>();
        InternalDrawCamera(ActiveCam, Color, LifeTime);
    }

    /// <summary>
    /// Draw a 3D representation of a camera
    /// </summary>
    /// /// <param name="Cam">Camera to draw</param>
    /// <param name="Color">Camera representation color</param>
    /// <param name="Scale">Scale of the shape</param>
    /// <param name="LifeTime">Shape life time</param>
    public static void DrawCamera(Camera Cam, Color Color, float LifeTime = 0.0f)
    {
        InternalDrawCamera(Cam, Color, LifeTime);
    }

    /// <summary>
    /// Draw a grid in 3D space
    /// </summary>
    /// <param name="Position">Position of the grid</param>
    /// <param name="GridSize">Grid size</param>
    /// <param name="CellSize">Grid cell size</param>
    /// <param name="LifeTime">Grid life time</param>
    public static void DrawGrid(Vector3 Position, float GridSize, float CellSize, float LifeTime)
    {
        DrawGrid(Position, Vector3.up, GridSize, CellSize, LifeTime);
    }

    /// <summary>
    /// Draw a grid in 3D space
    /// </summary>
    /// <param name="Position">Position of the grid</param>
    /// <param name="Normal">Normal vector of the grid</param>
    /// <param name="GridSize">Grid size</param>
    /// <param name="CellSize">Grid cell size</param>
    /// <param name="LifeTime">Grid life time</param>
    public static void DrawGrid(Vector3 Position, Vector3 Normal, float GridSize, float CellSize, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        Color MajorLinesColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
        Color OtherLinesColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
        float HalfGridSize = GridSize / 2.0f;
        Quaternion GridRot = Quaternion.LookRotation(Normal);

        // Draw rectangle
        InternalDrawLine(new Vector3(Position.x - HalfGridSize, Position.y - HalfGridSize, Position.z),
            new Vector3(Position.x - HalfGridSize, Position.y + HalfGridSize, Position.z),
            Position, GridRot, MajorLinesColor, LifeTime);

        InternalDrawLine(new Vector3(Position.x - HalfGridSize, Position.y + HalfGridSize, Position.z),
            new Vector3(Position.x + HalfGridSize, Position.y + HalfGridSize, Position.z),
            Position, GridRot, MajorLinesColor, LifeTime);

        InternalDrawLine(new Vector3(Position.x + HalfGridSize, Position.y + HalfGridSize, Position.z),
            new Vector3(Position.x + HalfGridSize, Position.y - HalfGridSize, Position.z),
            Position, GridRot, MajorLinesColor, LifeTime);

        InternalDrawLine(new Vector3(Position.x + HalfGridSize, Position.y - HalfGridSize, Position.z),
            new Vector3(Position.x - HalfGridSize, Position.y - HalfGridSize, Position.z),
            Position, GridRot, MajorLinesColor, LifeTime);

        // Draw centered axis
        InternalDrawLine(new Vector3(Position.x - HalfGridSize, Position.y, Position.z),
            new Vector3(Position.x + HalfGridSize, Position.y, Position.z),
            Position, GridRot, new Color(0.8f, 0.3f, 0.3f, 1.0f), LifeTime);
        InternalDrawLine(new Vector3(Position.x, Position.y - HalfGridSize, Position.z),
            new Vector3(Position.x, Position.y + HalfGridSize, Position.z),
            Position, GridRot, new Color(0.3f, 0.3f, 0.8f, 1.0f), LifeTime);

        int CellNum = (int) Mathf.Ceil((GridSize / CellSize) / 2.0f) - 1;

        // Draw grid lines
        for (int i = -CellNum; i <= CellNum; i++)
        {
            if (i == 0) continue;
            Vector3 V1 = new Vector3(Position.x + i * (CellSize), Position.y - HalfGridSize, Position.z);
            Vector3 V2 = new Vector3(Position.x + i * (CellSize), Position.y + HalfGridSize, Position.z);
            InternalDrawLine(V1, V2, Position, GridRot, OtherLinesColor, LifeTime);

            V1 = new Vector3(Position.x - HalfGridSize, Position.y + i * (CellSize), Position.z);
            V2 = new Vector3(Position.x + HalfGridSize, Position.y + i * (CellSize), Position.z);
            InternalDrawLine(V1, V2, Position, GridRot, OtherLinesColor, LifeTime);
        }

    }

    /// <summary>
    /// Draw a mesure tool to mesure distance between two points
    /// </summary>
    /// <param name="Start">Start position</param>
    /// <param name="End">End position</param>
    /// <param name="Color">Color of the mesure tool</param>
    /// <param name="LifeTime">Draw life time</param>
    public static void DrawDistance(Vector3 Start, Vector3 End, Color Color, float TextSize = 1.0f,
        float LifeTime = 0.0f)
    {
        float Dist = Vector3.Distance(Start, End);
        Vector3 DistTextPos = (Start + End) / 2.0f;
        float DistEndSize = 0.3f;
        InternalDrawLine(Start, End, DistTextPos, Quaternion.identity, Color, LifeTime);

        Vector3 DistDir = (End - Start).normalized;
        Vector3 RightDir = Vector3.Cross(DistDir, Vector3.up);

        InternalDrawLine(Start - RightDir * DistEndSize, Start + RightDir * DistEndSize, DistTextPos,
            Quaternion.identity, Color, LifeTime);
        InternalDrawLine(End - RightDir * DistEndSize, End + RightDir * DistEndSize, DistTextPos, Quaternion.identity,
            Color, LifeTime);

        DrawString3D(DistTextPos, Quaternion.LookRotation(Camera.main.transform.position - DistTextPos),
            Dist.ToString(".00"), TextAnchor.MiddleCenter, Color.white, TextSize, LifeTime);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    /// <summary>
    /// Draw RaycastHit structures infos
    /// </summary>
    /// <param name="Origin">The starting point of the ray in world coordinates</param>
    /// <param name="Direction">The direction of the ray</param>
    /// <param name="MaxDistance">The max distance the ray should check for collisions</param>
    /// <param name="HitInfos">Information about where the closest collider was hit</param>
    /// <param name="LifeTime">Draw life time</param>
    public static void DrawRaycastHit(Vector3 Origin, Vector3 Direction,
        [DefaultValue("Mathf.Infinity")] float MaxDistance, RaycastHit HitInfos, float LifeTime = 0.0f)
    {
        if (HitInfos.collider != null)
        {
            DrawLine(Origin, Origin + Direction.normalized * HitInfos.distance, Color.red, LifeTime);
            DrawLine(HitInfos.point, HitInfos.point + Direction.normalized * (MaxDistance - HitInfos.distance),
                Color.cyan, LifeTime);
            DrawDirectionalArrow(HitInfos.point, HitInfos.point + HitInfos.normal * 2.0f, 0.2f, Color.red, LifeTime);
            DrawSphere(HitInfos.point, 0.2f, 8, Color.red, LifeTime);
            DrawString3D((Origin + HitInfos.point) / 2.0f, HitInfos.distance.ToString(".00"), TextAnchor.MiddleCenter,
                Color.white, 1.0f, LifeTime);
            InternalDrawCollider(HitInfos.collider, Color.yellow, LifeTime);
            DrawBounds(HitInfos.collider.bounds, Color.yellow, LifeTime);
        }
        else
        {
            DrawLine(Origin, Origin + Direction.normalized * MaxDistance, Color.cyan, LifeTime);
        }
    }

    /// <summary>
    /// Log message text on screen
    /// </summary>
    /// <param name="LogMessage">Log message string</param>
    /// <param name="Color">Log messsage color</param>
    /// <param name="LifeTime">Log life time</param>
    public static void Log(string LogMessage, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        DrawDebugTools.Instance.m_LogMessagesList.Add(new DebugLogMessage(LogMessage, Color, LifeTime));
    }

    /// <summary>
    /// Simplified function to log message text on screen
    /// </summary>
    /// <param name="LogMessage">Log message string</param>
    /// <param name="LifeTime">Log life time</param>
    public static void Log(string LogMessage, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        DrawDebugTools.Instance.m_LogMessagesList.Add(new DebugLogMessage(LogMessage, Color.white, LifeTime));
    }



    /// <summary>
    /// Draw a float graph on screen
    /// </summary>
    /// <param name="UniqueGraphName">A unique name for the graph</param>
    /// <param name="FloatValueToDebug">Float variable to debug</param>
    /// <param name="GraphHalfMinMaxRange">Min graph value</param>
    /// <param name="AutoAdjustMinMaxRange">Max graph value</param>
    /// <param name="SamplesCount">Graph sample count (Default = 50)</param>
    public static void DrawFloatGraph(string UniqueGraphName, float FloatValueToDebug)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        bool IsFloatAlreadyExists = false;
        float TimeBeforeRemoveInactiveGraph = 2.0f;

        for (int i = 0; i < DrawDebugTools.Instance.m_FloatGraphsList.Count; i++)
        {
            if (DrawDebugTools.Instance.m_FloatGraphsList[i].m_UniqueFloatName == UniqueGraphName)
            {
                IsFloatAlreadyExists = true;
                Instance.m_FloatGraphsList[i].AddValue(FloatValueToDebug);
                break;
            }
        }

        if (!IsFloatAlreadyExists)
        {
            Instance.m_FloatGraphsList.Add(new DebugFloatGraph(UniqueGraphName,
                DrawDebugTools.Instance.m_FloatGraphSamplesCount, 1.0f, true, TimeBeforeRemoveInactiveGraph));
        }
    }

    /// <summary>
    /// Draw colliders compoenent on game object and its children
    /// </summary>
    /// <param name="Object">Game object that its colliders will be drawn</param>
    /// <param name="Color">Colliders color</param>
    /// <param name="LifeTime">Drawing time</param>
    public static void DrawObjectColliders(GameObject Object, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        Collider[] CollidersArray = Object.GetComponentsInChildren<Collider>();
        foreach (var ColliderItem in CollidersArray)
        {
            InternalDrawCollider(ColliderItem, Color, LifeTime);
        }
    }

    /// <summary>
    /// Draw bounds
    /// </summary>
    /// <param name="InBounds">Bounds to draw</param>
    /// <param name="Color">Drawing colorolor</param>
    /// <param name="LifeTime">Draw life time</param>
    public static void DrawBounds(Bounds InBounds, Color Color, float LifeTime = 0.0f)
    {
        DrawBox(InBounds.center, Quaternion.identity, InBounds.size, Color, LifeTime);
    }

    /// <summary>
    /// Draw quad with texture, used mainly for billboards. Do not call in Update.
    /// </summary>
    /// <param name="Billboard">Quad object to draw</param>
    public static void AddBillboardToDrawList(DebugBillboard Billboard)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        if (DrawDebugTools.Instance.m_DebugBillboardsList.Count != 0 &&
            DrawDebugTools.Instance.m_DebugBillboardsList.Contains(Billboard))
            return;

        DrawDebugTools.Instance.m_DebugBillboardsList.Add(Billboard);
    }

    public static void RemoveBillboardFromList(DebugBillboard Billboard)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        if (DrawDebugTools.Instance.m_DebugBillboardsList.Count > 0)
        {
            DrawDebugTools.Instance.m_DebugBillboardsList.Remove(Billboard);
        }
    }

    #region Private Internal Functions

    private static void InternalDrawCollider(Collider ColliderItem, Color Color, float LifeTime = 0.0f)
    {
        // Check if box
        if (ColliderItem.GetType() == typeof(BoxCollider))
        {
            BoxCollider BoxColliderComp = (BoxCollider) ColliderItem;

            // Get parent scale
            Vector3 ParentScale = Vector3.one;
            if (ColliderItem.transform.parent != null)
                ParentScale = ColliderItem.transform.parent.localScale;

            // Calculate position
            Vector3 ColliderPos = ColliderItem.transform.position +
                                  ColliderItem.transform.right * BoxColliderComp.center.x * ParentScale.x +
                                  ColliderItem.transform.forward * BoxColliderComp.center.z * ParentScale.z +
                                  ColliderItem.transform.up * BoxColliderComp.center.y * ParentScale.y;

            // Calculate scaled size
            float SizeX = BoxColliderComp.size.x * BoxColliderComp.transform.localScale.x * ParentScale.x;
            float SizeY = BoxColliderComp.size.y * BoxColliderComp.transform.localScale.y * ParentScale.y;
            float SizeZ = BoxColliderComp.size.z * BoxColliderComp.transform.localScale.z * ParentScale.z;

            Vector3 BoxSize = new Vector3(SizeX, SizeY, SizeZ);

            // Draw box
            DrawBox(ColliderPos, ColliderItem.transform.rotation, BoxSize, Color, LifeTime);
        }

        // Check if sphere
        if (ColliderItem.GetType() == typeof(SphereCollider))
        {
            SphereCollider SphereColliderComp = (SphereCollider) ColliderItem;

            // Get parent scale
            Vector3 ParentScale = Vector3.one;
            if (ColliderItem.transform.parent != null)
                ParentScale = ColliderItem.transform.parent.localScale;

            // Calculate position
            Vector3 ColliderPos = ColliderItem.transform.position +
                                  ColliderItem.transform.right * SphereColliderComp.center.x * ParentScale.x +
                                  ColliderItem.transform.forward * SphereColliderComp.center.z * ParentScale.z +
                                  ColliderItem.transform.up * SphereColliderComp.center.y * ParentScale.y;

            float ScaledRadius = SphereColliderComp.bounds.extents.x;

            // Draw shere
            DrawSphere(ColliderPos, ColliderItem.transform.rotation, ScaledRadius, 8, Color, LifeTime);
        }

        // Check if capsule
        if (ColliderItem.GetType() == typeof(CapsuleCollider))
        {
            CapsuleCollider CapsuleColliderComp = (CapsuleCollider) ColliderItem;

            // Get parent scale
            Vector3 ParentScale = Vector3.one;
            if (ColliderItem.transform.parent != null)
                ParentScale = ColliderItem.transform.parent.localScale;

            // Calculate position
            Vector3 ColliderPos = ColliderItem.transform.position +
                                  ColliderItem.transform.right * CapsuleColliderComp.center.x * ParentScale.x +
                                  ColliderItem.transform.forward * CapsuleColliderComp.center.z * ParentScale.z +
                                  ColliderItem.transform.up * CapsuleColliderComp.center.y * ParentScale.y;

            float ScaledRadius = CapsuleColliderComp.radius * ColliderItem.transform.lossyScale.x;
            float ScaledHeight = CapsuleColliderComp.height * 0.5f * ColliderItem.transform.lossyScale.y;

            // Draw shere
            DrawCapsule(ColliderPos, ScaledHeight, ScaledRadius, ColliderItem.transform.rotation, Color, LifeTime);
        }
    }

    private static void InternalDrawLine(Vector3 LineStart, Vector3 LineEnd, Vector3 Center, Quaternion Rotation,
        Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        DrawDebugTools.Instance.AddLine(new BatchedLine(LineStart, LineEnd, Center, Rotation, Color, LifeTime));
    }

    private static void InternalDrawCapsuleCircle(Vector3 Base, Vector3 X, Vector3 Z, Color Color, float Radius, int Segments, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        float AngleDelta = 2.0f * Mathf.PI / Segments;
        Vector3 LastPoint = Base + X * Radius;

        for (int i = 0; i < Segments; i++)
        {
            Vector3 Point = Base + (X * Mathf.Cos(AngleDelta * (i + 1)) + Z * Mathf.Sin(AngleDelta * (i + 1))) * Radius;
            DrawDebugTools.InternalDrawLine(LastPoint, Point, Base, Quaternion.identity, Color, LifeTime);
            LastPoint = Point;
        }
    }

    private static void InternalDrawCamera(Camera Camera, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        if (Camera == null) return;

        float CamBoxDepth = 0.4f;
        float CamBoxHeight = 0.3f;
        float CamBoxWidth = 0.2f;
        float CamCylRadius = 0.25f;
        float CamCylDistance = 0.55f;

        Vector3 CamPos = Camera.transform.position - Camera.transform.forward * CamBoxDepth;
        Quaternion CamRot = Camera.transform.rotation;
        DrawDebugTools.DrawSphere(Camera.transform.position, 0.05f, 4, Color, LifeTime);
        // Box
        DrawDebugTools.DrawBox(CamPos, CamRot, new Vector3(CamBoxWidth, CamBoxHeight, CamBoxDepth) * 2.0f, Color,
            LifeTime);

        // Two cylinders
        Vector3 V1 = CamPos + new Vector3(CamBoxWidth / 2.0f, (CamBoxHeight) + CamCylRadius, -CamCylDistance / 2.0f);
        Vector3 V2 = CamPos + new Vector3(-CamBoxWidth / 2.0f, (CamBoxHeight) + CamCylRadius, -CamCylDistance / 2.0f);

        InternalDrawCylinder(V1, V2, CamRot, CamPos, CamCylRadius, 8, Color, LifeTime);
        V1 += new Vector3(0.0f, 0.0f, CamCylDistance);
        V2 += new Vector3(0.0f, 0.0f, CamCylDistance);
        InternalDrawCylinder(V1, V2, CamRot, CamPos, CamCylRadius, 8, Color, LifeTime);

        // Zoom
        Vector3 Extent = new Vector3(CamBoxWidth * 0.7f, CamBoxHeight * 0.7f, CamBoxDepth * 0.7f);
        Vector3 Center = CamPos + new Vector3(0.0f, 0.0f, CamBoxDepth);

        InternalDrawLine(Center + new Vector3(Extent.x, Extent.y, 0.0f),
            Center + new Vector3(Extent.x, -Extent.y, 0.0f), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(Extent.x, -Extent.y, 0.0f),
            Center + new Vector3(-Extent.x, -Extent.y, 0.0f), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Extent.x, -Extent.y, 0.0f),
            Center + new Vector3(-Extent.x, Extent.y, 0.0f), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Extent.x, Extent.y, 0.0f),
            Center + new Vector3(Extent.x, Extent.y, 0.0f), CamPos, CamRot, Color, LifeTime);

        float ZoomDepth = CamBoxDepth;
        float v = 3.0f;
        InternalDrawLine(Center + new Vector3(Extent.x * v, Extent.y * v, ZoomDepth),
            Center + new Vector3(Extent.x * v, -Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(Extent.x * v, -Extent.y * v, ZoomDepth),
            Center + new Vector3(-Extent.x * v, -Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Extent.x * v, -Extent.y * v, ZoomDepth),
            Center + new Vector3(-Extent.x * v, Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Extent.x * v, Extent.y * v, ZoomDepth),
            Center + new Vector3(Extent.x * v, Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);

        InternalDrawLine(Center + new Vector3(Extent.x, Extent.y, 0.0f),
            Center + new Vector3(Extent.x * v, Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(Extent.x, -Extent.y, 0.0f),
            Center + new Vector3(Extent.x * v, -Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Extent.x, -Extent.y, 0.0f),
            Center + new Vector3(-Extent.x * v, -Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);
        InternalDrawLine(Center + new Vector3(-Extent.x, Extent.y, 0.0f),
            Center + new Vector3(-Extent.x * v, Extent.y * v, ZoomDepth), CamPos, CamRot, Color, LifeTime);
    }

    private static void InternalDrawCylinder(Vector3 Start, Vector3 End, Quaternion Rotation, Vector3 Center,
        float Radius, int Segments, Color Color, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        Segments = Mathf.Max(Segments, 4);

        Vector3 CylinderUp = ((End + new Vector3(0.0f, 0.0f, 0.01f)) - Start).normalized;
        Vector3 CylinderRight = Vector3.Cross(Vector3.up, CylinderUp).normalized;
        Vector3 CylinderForward = Vector3.Cross(CylinderRight, CylinderUp).normalized;
        float CylinderHeight = (End - Start).magnitude;

        float AngleInc = 2.0f * Mathf.PI / (float) Segments;

        // Debug End
        float Angle = 0.0f;
        Vector3 P_1;
        Vector3 P_2;
        Vector3 P_3;
        Vector3 P_4;

        Vector3 RotatedVect;
        for (int i = 0; i < Segments; i++)
        {
            RotatedVect = Quaternion.AngleAxis(Mathf.Rad2Deg * Angle, CylinderUp) * CylinderRight * Radius;

            P_1 = Start + RotatedVect;
            P_2 = P_1 + CylinderUp * CylinderHeight;

            // Draw lines
            DrawDebugTools.InternalDrawLine(P_1, P_2, Center, Rotation, Color, LifeTime);

            Angle += AngleInc;
            RotatedVect = Quaternion.AngleAxis(Mathf.Rad2Deg * Angle, CylinderUp) * CylinderRight * Radius;

            P_3 = Start + RotatedVect;
            P_4 = P_3 + CylinderUp * CylinderHeight;

            // Draw lines
            DrawDebugTools.InternalDrawLine(P_1, P_3, Center, Rotation, Color, LifeTime);
            DrawDebugTools.InternalDrawLine(P_2, P_4, Center, Rotation, Color, LifeTime);
        }
    }

    private static void InternalAddDebugText(string Text, TextAnchor Anchor, Vector3 Position, Quaternion Rotation,
        Color Color, float Size, float LifeTime)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        DrawDebugTools.Instance.m_DebugTextesList.Add(new DebugText(Text, Anchor, Position, Rotation, Color, Size,
            LifeTime));
    }

    private static void InternalDrawHalfCircle(Vector3 Base, Vector3 X, Vector3 Z, Color Color, float Radius, int Segments, float LifeTime = 0.0f)
    {
        if (DrawDebugTools.Instance == null)
            return;
        
        float AngleDelta = 2.0f * Mathf.PI / Segments;
        Vector3 LastPoint = Base + X * Radius;

        for (int i = 0; i < (Segments / 2); i++)
        {
            Vector3 Point = Base + (X * Mathf.Cos(AngleDelta * (i + 1)) + Z * Mathf.Sin(AngleDelta * (i + 1))) * Radius;
            DrawDebugTools.InternalDrawLine(LastPoint, Point, Base, Quaternion.identity, Color, LifeTime);
            LastPoint = Point;
        }
    }

    #endregion

    #endregion

    #region ========== Handle Drawing Lines/Quads ==========

    private void HandleDrawingListOfLines()
    {
        if (m_BatchedLines.Count == 0)
            return;
        
        
        // Check material is set
        if (!m_LineMaterial)
        {
            InitializeMaterials();
        }

        // Draw lines
        m_Mesh = new Mesh();

        List<Vector3> InVerticesList = new List<Vector3>();
        int[] InIndicesArray = new int[m_BatchedLines.Count * 2];
        Color[] InColorsArray = new Color[m_BatchedLines.Count * 2];


        Matrix4x4 M = Camera.main.projectionMatrix;

        for (int i = 0; i < m_BatchedLines.Count; i++)
        {
            M.SetTRS(Vector3.zero, m_BatchedLines[i].Rotation, Vector3.one);
            Vector3 S = m_BatchedLines[i].Start - m_BatchedLines[i].PivotPoint;
            Vector3 E = m_BatchedLines[i].End - m_BatchedLines[i].PivotPoint;

            Vector3 ST = M.MultiplyPoint(S);
            Vector3 ET = M.MultiplyPoint(E);

            ST += m_BatchedLines[i].PivotPoint;
            ET += m_BatchedLines[i].PivotPoint;

            InVerticesList.Add(ST);
            InVerticesList.Add(ET);

            InIndicesArray[2 * i] = 2 * i;
            InIndicesArray[2 * i + 1] = 2 * i + 1;
            
            InColorsArray[2 * i] = m_BatchedLines[Mathf.FloorToInt(i)].Color;
            InColorsArray[2 * i + 1] = m_BatchedLines[Mathf.FloorToInt(i)].Color;
        }

        m_Mesh.SetVertices(InVerticesList);
        m_Mesh.colors = InColorsArray;
        m_Mesh.SetIndices(InIndicesArray, MeshTopology.Lines, 0);
        Graphics.DrawMesh(m_Mesh, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0.0f), m_LineMaterial, 1);

        // Update lines
        for (int i = m_BatchedLines.Count - 1; i >= 0; i--)
        {
            m_BatchedLines[i].RemainLifeTime -= Time.deltaTime;
            if (m_BatchedLines[i].RemainLifeTime <= 0.0f)
            {
                m_BatchedLines.RemoveAt(i);
            }
        }
    }

    private void HandleDrawingListOfBillboards()
    {
        if (m_DebugBillboardsList.Count == 0)
            return;

        // Check material is set
        if (!m_QuadMaterial)
        {
            InitializeMaterials();
        }

        // Draw quads
        m_Mesh = new Mesh();

        for (int i = 0; i < m_DebugBillboardsList.Count; i++)
        {
            if (m_DebugBillboardsList[i] == null || m_DebugBillboardsList[i].IsHidden == true)
            {
                break;
            }
            
            // Set width and height
            float width = m_DebugBillboardsList[i].Width;
            float height = m_DebugBillboardsList[i].Height;

            // Set vertices, indices, normals and uvs
            Vector3[] InVerticesArray = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(0, height, 0),
                new Vector3(width, height, 0)
            };

            int[] InIndicesArray = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };

            Vector3[] NormalsArray = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };

            Vector2[] UvsArrays = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            // Set mesh variables
            m_Mesh.SetVertices(InVerticesArray.ToList());
            m_Mesh.SetIndices(InIndicesArray, MeshTopology.Triangles, 0);
            m_Mesh.normals = NormalsArray;
            m_Mesh.uv = UvsArrays;

            if (m_DebugBillboardsList[i].QuadTexture)
            {
                m_QuadMatPropertyBlock.SetTexture("_MainTex", m_DebugBillboardsList[i].QuadTexture);
            }

            // Set rotation
            Quaternion Rotation = Quaternion.Euler(m_DebugBillboardsList[i].EulerRotation);
            if (m_DebugBillboardsList[i].IsBillboard)
            {
                Rotation = GetCamLookAtRotation(m_DebugBillboardsList[i].Position, true);
            }

            // Draw billboard quad
            Graphics.DrawMesh(m_Mesh, m_DebugBillboardsList[i].Position, Rotation, m_QuadMaterial, 1, null, 0, m_QuadMatPropertyBlock);
        }
    }

    public void AddLine(BatchedLine Line)
    {
        DrawDebugTools.Instance.m_BatchedLines.Add(Line);
    }
    
    public void AddRangeLine(List<BatchedLine> LinesList)
    {
        DrawDebugTools.Instance.m_BatchedLines.AddRange(LinesList);
    }

    private void HandleDrawingListOfTextes()
    {
        // Draw 3D text
        if (m_3DTextesParent == null)
        {
            m_3DTextesParent = new GameObject("3DTextes");
            m_3DTextesParent.transform.SetParent(transform);
            m_3DTextesParent.transform.localPosition = Vector3.zero;
        }

        // Delete unused textes mesh
        m_3DTextesList.Clear();
        m_3DTextesList = m_3DTextesParent.GetComponentsInChildren<TextMesh>().ToList<TextMesh>();

        int DiffNum = Mathf.Abs(m_3DTextesList.Count - m_DebugTextesList.Count);

        if (DiffNum != 0)
        {
            if (m_3DTextesList.Count > m_DebugTextesList.Count)
            {
                for (int i = m_3DTextesList.Count - 1; i >= m_3DTextesList.Count - DiffNum; i--)
                {
                    if (i < m_3DTextesList.Count)
                    {
                        GameObject TextObj = m_3DTextesList[i].gameObject;
                        Destroy(TextObj);
                    }
                }

                m_3DTextesList.RemoveRange(m_3DTextesList.Count - DiffNum, DiffNum);
            }
            else
            {
                for (int i = 0; i < DiffNum; i++)
                {
                    m_3DTextesList.Add(Instantiate3DText());
                }
            }
        }

        int m_DefaultFontSize = 48;

        for (int i = 0; i < m_3DTextesList.Count; i++)
        {
            m_3DTextesList[i].text = m_DebugTextesList[i].m_TextString;
            m_3DTextesList[i].transform.position = m_DebugTextesList[i].m_TextPosition;
            m_3DTextesList[i].transform.rotation = m_DebugTextesList[i].m_TextRotation;
            m_3DTextesList[i].transform.localScale = new Vector3(-0.05f, 0.05f, 0.05f);
            m_3DTextesList[i].fontSize = m_DefaultFontSize * (int) m_DebugTextesList[i].m_Size;
            m_3DTextesList[i].color = m_DebugTextesList[i].m_TextColor;
            m_3DTextesList[i].anchor = m_DebugTextesList[i].m_TextAnchor;
            m_3DTextesList[i].name = "3DText-" + m_DebugTextesList[i].m_TextString;
        }

        // Update text life time
        for (int i = m_DebugTextesList.Count - 1; i >= 0; i--)
        {
            m_DebugTextesList[i].m_RemainLifeTime -= Time.deltaTime;
            if (m_DebugTextesList[i].m_RemainLifeTime <= 0.0f)
            {
                m_DebugTextesList.RemoveAt(i);
            }
        }
    }

    private void HandleDrawingListOfFloatGraphs()
    {
        if (m_DDTCanvas == null)
        {
            if (GameObject.FindObjectOfType<DDTCanvas>())
                m_DDTCanvas = GameObject.FindObjectOfType<DDTCanvas>();
            else
                m_DDTCanvas = GameObject.Instantiate(m_DDTCanvasPrefab, transform).GetComponent<DDTCanvas>();
        }

        m_DDTCanvas.UpdateGraphFloats(m_FloatGraphsList);

        // Update text life time
        for (int i = m_FloatGraphsList.Count - 1; i >= 0; i--)
        {
            m_FloatGraphsList[i].m_TimeBeforeRemoveCounter += Time.deltaTime;
            if (m_FloatGraphsList[i].m_TimeBeforeRemoveCounter >= m_FloatGraphsList[i].m_TimeBeforeRemove)
            {
                m_FloatGraphsList.RemoveAt(i);
            }
        }
    }

    private void HandleListOfLogMessagesList()
    {
        if (m_DDTCanvas == null)
        {
            if (GameObject.FindObjectOfType<DDTCanvas>())
                m_DDTCanvas = GameObject.FindObjectOfType<DDTCanvas>();
            else
                m_DDTCanvas = GameObject.Instantiate(m_DDTCanvasPrefab, transform).GetComponent<DDTCanvas>();
        }

        m_DDTCanvas.UpdateLogTextes(m_LogMessagesList);

        // Update log life time
        for (int i = m_LogMessagesList.Count - 1; i >= 0; i--)
        {
            m_LogMessagesList[i].m_RemainingTime -= Time.fixedUnscaledDeltaTime;
            if (m_LogMessagesList[i].m_RemainingTime <= 0.0f)
            {
                m_LogMessagesList.RemoveAt(i);
            }
        }
    }

    private TextMesh Instantiate3DText()
    {
        GameObject TextMeshObj = GameObject.Instantiate(m_3DTextePrefab);
        TextMeshObj.transform.SetParent(m_3DTextesParent.transform);
        TextMeshObj.name = "MeshMat_ValText_" + TextMeshObj.GetInstanceID();
        return TextMeshObj.GetComponent<TextMesh>();
    }

    #endregion

    #region ========== Helper Functions ==========

    private Vector3 GetIntersectionPointOfPlanes(Plane Plane_1, Plane Plane_2, Plane Plane_3)
    {
        return ((-Plane_1.distance * Vector3.Cross(Plane_2.normal, Plane_3.normal)) +
                (-Plane_2.distance * Vector3.Cross(Plane_3.normal, Plane_1.normal)) +
                (-Plane_3.distance * Vector3.Cross(Plane_1.normal, Plane_2.normal))) /
               (Vector3.Dot(Plane_1.normal, Vector3.Cross(Plane_2.normal, Plane_3.normal)));
    }

    public int GetFloatGraphSamplesCount()
    {
        return m_FloatGraphSamplesCount;
    }

    public void FlushDebugLines()
    {
        // Delete all lines
        for (int i = m_BatchedLines.Count - 1; i >= 0; i--)
        {
            m_BatchedLines.RemoveAt(i);
        }
    }

    private Quaternion GetCamLookAtRotation(Vector3 Target, bool IsInverted = false)
    {
        if(Camera.main == null) 
            return Quaternion.identity;
        return (Quaternion.LookRotation((Camera.main.transform.position - Target) * (IsInverted?-1.0f:1.0f), Vector3.up));
    }

    #endregion
}


#region ========== Enums / Structures / Helper Classes ==========
[System.Serializable]
public class BatchedLine
{
    public Vector3 Start;
    public Vector3 End;
    public Vector3 PivotPoint;
    public Quaternion Rotation;
    public Color Color;
    public float RemainLifeTime;
    public bool IsEnabled = false;
    
    public BatchedLine(Vector3 InStart, Vector3 InEnd, Vector3 InPivotPoint, Quaternion InRotation, Color InColor, float InRemainLifeTime)
    {
        Start = InStart;
        End = InEnd;
        PivotPoint = InPivotPoint;
        Rotation = InRotation;
        Color = InColor;
        RemainLifeTime = InRemainLifeTime;
        IsEnabled = true;
    }
};

[System.Serializable]
public class DebugBillboard
{
    public Texture m_QuadTexture;
    private Vector3 m_Position;
    private Vector3 m_EulerRotation;
    private float m_Width = 1.0f;
    private float m_Height = 1.0f;
    private bool m_IsHidden = false;
    private bool m_IsBillboard = true;


    public Texture QuadTexture
    {
        get => m_QuadTexture;
        set => m_QuadTexture = value;
    }

    public Vector3 Position
    {
        get => m_Position;
        set => m_Position = value;
    }

    public Vector3 EulerRotation
    {
        get => m_EulerRotation;
        set => m_EulerRotation = value;
    }

    public float Width
    {
        get => m_Width == 0.0f ? 1.0f : m_Width;
        set => m_Width = value;
    }

    public float Height
    {
        get => m_Height == 0.0f ? 1.0f : m_Height;
        set => m_Height = value;
    }

    public bool IsHidden
    {
        get => m_IsHidden;
        set => m_IsHidden = value;
    }
    
    public bool IsBillboard
    {
        get => m_IsBillboard;
        set => m_IsBillboard = value;
    }

    public DebugBillboard(Texture QuadTexture)
    {
        this.m_QuadTexture = QuadTexture;
        m_Width = 1.0f;
        m_Height = 1.0f;
        m_IsHidden = true;
    }

    public DebugBillboard(Texture QuadTexture, Vector3 Position, Vector3 EulerRotation)
    {
        this.m_QuadTexture = QuadTexture;
        this.m_Position = Position;
        this.m_EulerRotation = EulerRotation;
    }
    
    public DebugBillboard(Texture QuadTexture, Vector3 Position, Vector3 EulerRotation, float Width = 1.0f, float Height = 1.0f)
    {
        this.m_QuadTexture = QuadTexture;
        this.m_Position = Position;
        this.m_EulerRotation = EulerRotation;
        this.m_Width = Width;
        this.m_Height = Height;
    }
    
    public DebugBillboard(Texture QuadTexture, Vector3 Position, Vector3 EulerRotation, float Width = 1.0f, float Height = 1.0f, bool IsHidden = true, bool IsBillboard = true)
    {
        this.m_QuadTexture = QuadTexture;
        this.m_Position = Position;
        this.m_EulerRotation = EulerRotation;
        this.m_Width = Width;
        this.m_Height = Height;
        this.m_IsHidden = IsHidden;
        this.m_IsBillboard = IsBillboard;
    }
}

public enum EDrawPlaneAxis
{
    XZ,
    XY,
    YZ
};

[System.Serializable]
public class DebugFloatGraph
{
    public string m_UniqueFloatName = "";
    public int m_SamplesCount = 20;
    public List<float> m_FloatValuesList;
    public float m_GraphValueLengh;
    public float m_MinValue;
    public float m_MaxValue;
    public bool m_AutoAdjustMinMaxRange = false;
    public float m_TimeBeforeRemove;
    public float m_TimeBeforeRemoveCounter = 0.0f;

    public DebugFloatGraph()
    {
    }

    public DebugFloatGraph(string UniqueFloatName, int SamplesCount, float GraphValueLengh, bool AutoAdjustMinMaxRange, float TimeBeforeRemove)
    {
        m_UniqueFloatName = UniqueFloatName;
        m_SamplesCount = SamplesCount;
        m_FloatValuesList = new List<float>();
        m_GraphValueLengh = GraphValueLengh;
        m_AutoAdjustMinMaxRange = AutoAdjustMinMaxRange;
        m_TimeBeforeRemove = TimeBeforeRemove;
    }

    public void AddValue(float NewFloatVal)
    {
        // Add value
        m_FloatValuesList.Add(NewFloatVal);

        // Set min and max
        if (Mathf.Abs(GetMaximumValue()) > Mathf.Abs(GetMinimumValue()))
        {
            m_MaxValue = Mathf.Abs(GetMaximumValue());
            m_MinValue = -Mathf.Abs(GetMaximumValue());
        }
        else
        {
            m_MaxValue = Mathf.Abs(GetMinimumValue());
            m_MinValue = -Mathf.Abs(GetMinimumValue());
        }

        // Auto adjust graph length
        if (m_AutoAdjustMinMaxRange)
        {
            if (Mathf.Abs(NewFloatVal) > m_GraphValueLengh)
            {
                m_GraphValueLengh = Mathf.Abs(NewFloatVal);
            }
        }

        // Remove first float from the array if we rech max samples
        if (GetDidReachMaxSamples())
            m_FloatValuesList.RemoveAt(0);

        // Reset counter
        m_TimeBeforeRemoveCounter = 0.0f;
    }

    public bool GetDidReachMaxSamples()
    {
        return m_FloatValuesList.Count > m_SamplesCount;
    }

    public float GetMinimumValue()
    {
        float SmallestValue = Mathf.Infinity;
        for (int i = 0; i < m_FloatValuesList.Count; i++)
        {
            if (m_FloatValuesList[i] < SmallestValue)
            {
                SmallestValue = m_FloatValuesList[i];
            }
        }
        return SmallestValue;
    }

    public float GetMaximumValue()
    {
        float BiggestValue = -Mathf.Infinity;
        for (int i = 0; i < m_FloatValuesList.Count; i++)
        {
            if (m_FloatValuesList[i] > BiggestValue)
            {
                BiggestValue = m_FloatValuesList[i];
            }
        }
        return BiggestValue;
    }
}

[System.Serializable]
public class DebugLogMessage
{
    public string   m_LogMessageText;
    public Color    m_Color;
    public float    m_RemainingTime;

    public DebugLogMessage(string LogMessageText, Color Color, float LifeTime)
    {
        m_LogMessageText = LogMessageText;
        m_Color = Color;
        m_RemainingTime = LifeTime;
    }
}

[System.Serializable]
public class DebugText
{
    public string m_TextString;
    public TextAnchor m_TextAnchor;
    public Vector3 m_TextPosition;
    public Quaternion m_TextRotation;
    public Color m_TextColor;
    public float m_Size;
    public float m_RemainLifeTime;

    public DebugText()
    {
    }

    public DebugText(string Text, TextAnchor TextAnchor, Vector3 TextPosition, Quaternion TextRotation, Color Color, float Size, float LifeTime)
    {
        m_TextString = Text;
        m_TextAnchor = TextAnchor;
        m_TextPosition = TextPosition;
        m_TextRotation = TextRotation;
        m_TextColor = Color;
        m_Size = Size;
        m_RemainLifeTime = LifeTime;
    }

    // Get text anchored position
    public Vector3 GetTextOriginPosition(Font TextFont)
    {
        Vector3 OriginPos = m_TextPosition;

        float TextWidth = 0.0f;
        float TextHeight = 0.0f;

        // Get text size
        char[] CharsArray = m_TextString.ToCharArray();
        for (int i = 0; i < CharsArray.Length; i++)
        {
            CharacterInfo CharInfos;
            TextFont.GetCharacterInfo(CharsArray[i], out CharInfos);
            TextWidth += CharInfos.advance * m_Size;
            if (i == 0)
                TextHeight = CharInfos.glyphHeight * m_Size;
        }

        switch (m_TextAnchor)
        {
            case TextAnchor.UpperLeft:
                OriginPos += new Vector3(0.0f, -TextHeight, 0.0f);
                break;
            case TextAnchor.UpperCenter:
                OriginPos += new Vector3(TextWidth / 2.0f, -TextHeight, 0.0f);
                break;
            case TextAnchor.UpperRight:
                OriginPos += new Vector3(-TextWidth, -TextHeight, 0.0f);
                break;
            case TextAnchor.MiddleLeft:
                OriginPos += new Vector3(0.0f, -TextHeight / 2.0f, 0.0f);
                break;
            case TextAnchor.MiddleCenter:
                OriginPos += new Vector3(TextWidth / 2.0f, -TextHeight / 2.0f, 0.0f);
                break;
            case TextAnchor.MiddleRight:
                OriginPos += new Vector3(-TextWidth, -TextHeight / 2.0f, 0.0f);
                break;
            case TextAnchor.LowerLeft:
                // Default position
                break;
            case TextAnchor.LowerCenter:
                OriginPos += new Vector3(TextWidth / 2.0f, 0.0f, 0.0f);
                break;
            case TextAnchor.LowerRight:
                OriginPos += new Vector3(-TextWidth, 0.0f, 0.0f);
                break;
            default:
                break;
        }
        return OriginPos;
    }
}

#endregion

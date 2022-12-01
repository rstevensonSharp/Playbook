using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GizmoAxis
{
    public GameObject gameObject;
    public IDictionary<string, GameObject> axis;

    public GizmoAxis(GameObject n_gameObject)
    {
        this.gameObject = n_gameObject;
        if (n_gameObject == null) { return; }
        axis = new Dictionary<string, GameObject>() {
            {"",n_gameObject},
            {"X",n_gameObject.transform.Find("X").gameObject},
            {"Y",n_gameObject.transform.Find("Y").gameObject},
            {"Z",n_gameObject.transform.Find("Z").gameObject}
        };
    }
    public GameObject this[string index]
    {
        get => axis[index];
    }

}

public class Gizmo : MonoBehaviour
{
    public Transform target;
    public Vector3 currentAxis;
    private Vector3 axisOffset;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;

    private Vector3 prev_MousePosition;
    private Vector3 targetScreen; //the screen position of the target

    GizmoAxis gizmoPosition;
    GizmoAxis gizmoRotation;
    GizmoAxis gizmoScale;
    GizmoAxis[] gizmos = new GizmoAxis[3];
    GizmoAxis currentGizmo;
    private string[] axes = { "X", "Y", "Z" };

    public AudioClip moveSound;
    public AudioClip selectedSound;
    private AudioSource audioSource;

    private int gizmoLayer;

    public bool isActive { get { return gameObject.activeInHierarchy; } }
    public void SetActive(bool state=true)
    {
        gameObject.SetActive(state);
        if (state == false) {
            target = null;
        }
    }
    //Set the target that this gizmo will manipulate
    public void SetTarget(Transform newTarget)
    {
        gameObject.SetActive(true);
        target = newTarget;
        transform.position = target.position;
        if (newTarget != null) {
            audioSource.PlayOneShot(selectedSound);
        }
    }

    void Awake()
    {
        axisOffset = Vector3.zero;
        prev_MousePosition = Vector3.zero;

        gizmoPosition = new GizmoAxis(transform.Find("Position").gameObject);
        gizmoRotation = new GizmoAxis(transform.Find("Rotation").gameObject);
        gizmoScale = new GizmoAxis(transform.Find("Scale").gameObject);
        gizmos[0] = gizmoPosition;
        gizmos[1] = gizmoRotation;
        gizmos[2] = gizmoScale;
        currentGizmo = null;
        gizmoLayer = 1 << LayerMask.NameToLayer("UI");

        UpdateLineRenderers();
        SetColors();
        audioSource = GetComponent<AudioSource>();
    }

    //Color based on the axis, and whether that object is being hovered over
    void SetColors(GizmoAxis currentGizmo=null,string selected="")
    {
        IDictionary<string,Color> colors = new Dictionary<string, Color>() {
            {"X",Color.red },
            {"Y",Color.green },
            {"Z",Color.blue }
        };
        foreach (GizmoAxis gizmo in gizmos) {
            foreach (var axis in axes) {
                float modifier = 0.5f;
                if (currentGizmo != null) {
                    if (gizmo.gameObject == currentGizmo.gameObject && axis == selected) {
                        modifier = 1;
                    }
                }
                gizmo[axis].GetComponent<Renderer>().material.color = colors[axis] * modifier;
                gizmo[axis].GetComponent<LineRenderer>().SetColors(colors[axis], colors[axis] * modifier);
                gizmo[axis].GetComponent<LineRenderer>().widthMultiplier = 0.025f;

            }
        }
    }
    //Make gizmo handles visible if there is nothing selected, otherwise only make the current gizmo's current axis visible
    void SetVisibility(GizmoAxis currentGizmo = null, string selected = "")
    {
        foreach (GizmoAxis gizmo in gizmos) {
            foreach (var axis in axes) {
                bool visible = currentGizmo==null;
                if (currentGizmo != null) {
                    if (gizmo.gameObject == currentGizmo.gameObject && axis == selected) {
                        visible = true;
                    }
                }
                gizmo[axis].GetComponent<Renderer>().enabled = visible;
                gizmo[axis].GetComponent<LineRenderer>().enabled = visible;

            }
        }
    }

    void UpdateLineRenderers()
    {
        foreach (var axis in axes) {
            gizmoPosition[axis].GetComponent<LineRenderer>().SetPosition(0, gizmoPosition[axis].transform.position);
            gizmoPosition[axis].GetComponent<LineRenderer>().SetPosition(1, gizmoPosition[axis].transform.position + gizmoPosition[axis].transform.forward*1.5f);

            gizmoScale[axis].GetComponent<LineRenderer>().SetPosition(0, gizmoScale[axis].transform.position);
            gizmoScale[axis].GetComponent<LineRenderer>().SetPosition(1, gizmoScale[axis].transform.position + gizmoScale[axis].transform.forward * 1f);
        }
    }

    //Move object to desiredPosition, but only on the specific axis
    void HandlePosition(Vector3 desiredPosition)
    {
        if (currentAxis.x == 0) {
            desiredPosition.x = target.position.x;
        }
        if (currentAxis.y == 0) {
            desiredPosition.y = target.position.y;
        }
        if (currentAxis.z == 0) {
            desiredPosition.z = target.position.z;
        }
        target.position = desiredPosition;
    }

    //Set the scale of the axis by getting the distance between the 3D mouse and the center of the target
    void HandleScale(Vector3 desiredPosition,Vector3 targetScreen)
    {
        Vector3 targetPosition = Camera.main.ScreenToWorldPoint(targetScreen);

        Vector3 newScale = target.localScale;
        if (currentAxis.x != 0) {
            newScale.x = originalScale.x * (desiredPosition - targetPosition).magnitude;
        }
        if (currentAxis.y != 0) {
            newScale.y = originalScale.y * (desiredPosition - targetPosition).magnitude;
        }
        if (currentAxis.z != 0) {
            newScale.z = originalScale.z * (desiredPosition - targetPosition).magnitude;
        }
        target.localScale = newScale;
    }

    //Rotate based on how far the mouse has moved
    void HandleRotation(Vector3 desiredPosition, Vector3 mousePoint)
    {
        var delta = Input.mousePosition - prev_MousePosition;
        target.Rotate(new Vector3(-currentAxis.x*delta.y, -currentAxis.y*delta.x,-currentAxis.z*delta.y),Space.World);
    }
    //Convert vector to string
    string GetAxisName(Vector3 axis)
    {
        if (axis == Vector3.right)
            { return "X"; }
        else if (axis == Vector3.up)
            { return "Y"; }
        else if (axis == Vector3.forward)
            { return "Z"; }
        else { return ""; }
    }

    //Raycast to find gizmo objects
    RaycastHit CheckForGizmoHandle(Ray ray)
    {
        RaycastHit hit = new RaycastHit();
        if (currentAxis == Vector3.zero) {
            if (Physics.Raycast(ray, out hit, 100, gizmoLayer)) {
                var axisName = hit.transform.gameObject.name;
                currentGizmo = gizmoPosition;
                if (hit.transform.parent.gameObject == gizmoRotation.gameObject) {
                    currentGizmo = gizmoRotation;
                }
                else if (hit.transform.parent.gameObject == gizmoScale.gameObject) {
                    currentGizmo = gizmoScale;
                }
                SetColors(currentGizmo, axisName);
            }
            else {
                SetColors();
            }
        }
        return hit;
    }


    void HandleGizmoSelection(Ray ray,RaycastHit hit)
    {

        if (Input.GetMouseButtonDown(0)) {
            if (hit.collider != null) {
                //Figure out which axis was selected
                var axisName = hit.transform.gameObject.name;
                switch (axisName) {
                    default:
                        currentAxis = Vector3.zero;
                        break;
                    case "X":
                        currentAxis = Vector3.right;
                        break;
                    case "Y":
                        currentAxis = Vector3.up;
                        break;
                    case "Z":
                        currentAxis = Vector3.forward;
                        break;
                }
                //find the offset of the mouse position relative to the center of the target
                axisOffset = hit.point - transform.position;
                axisOffset = axisOffset.normalized / 2;
                targetScreen = Camera.main.WorldToScreenPoint(target.position);
                axisOffset = target.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, targetScreen.z));
                
                //Set original values for resetting, and for scaling
                originalPosition = target.position;
                originalRotation = target.rotation;
                originalScale = target.localScale;
                prev_MousePosition = Input.mousePosition;
            }
            //If we did not select a gizmo, see if we selected an object that can be transformed
            else {
                if (Physics.Raycast(ray, out hit, 100, ~gizmoLayer)) {
                    SetTarget(hit.collider.transform);
                }
                else {
                    SetActive(false);
                }
            }
        }
        //Deselect gizmo axis
        else if (Input.GetMouseButtonUp(0)) {
            SetVisibility();
            currentAxis = Vector3.zero;
            audioSource.Stop();
        }
    }

    void HandleGizmoMovement()
    {
        if (currentAxis.magnitude > 0) {
            //Convert mouse position to world position
            targetScreen = Camera.main.WorldToScreenPoint(target.position);
            Vector3 mousePoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, targetScreen.z);
            Vector3 desiredPosition = Camera.main.ScreenToWorldPoint(mousePoint) + axisOffset;

            //If we are moving, perform transforms based on currentGizmo
            if ((Vector3.Magnitude(prev_MousePosition - Input.mousePosition) > .01f)) {
                if (!audioSource.isPlaying) {
                    audioSource.loop = true;
                    audioSource.clip = moveSound;
                    audioSource.Play();
                }
                SetVisibility(currentGizmo, GetAxisName(currentAxis));

                if (currentGizmo == gizmoPosition) {
                    HandlePosition(desiredPosition);
                }
                else if (currentGizmo == gizmoScale) {
                    HandleScale(desiredPosition - axisOffset, targetScreen);
                }
                else if (currentGizmo == gizmoRotation) {
                    HandleRotation(desiredPosition, mousePoint);
                }
            }
            else {
                audioSource.Stop();
            }
            prev_MousePosition = Input.mousePosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Ignore if there is no current target, else set position to that target
        if (target == null) {
            return;
        }
        transform.position = target.position;

        UpdateLineRenderers();

        //Cast a ray from the camera to the mouse
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = CheckForGizmoHandle(ray);

        HandleGizmoSelection(ray,hit);
        HandleGizmoMovement();

    }

}

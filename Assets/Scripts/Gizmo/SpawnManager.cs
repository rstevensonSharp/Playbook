using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SpawnManager : MonoBehaviour
{
    private GameObject currentObject;
    private GizmoManager gizmoManager;

    public AudioClip clickSound;
    public AudioClip placeSound;
    public AudioClip failSound;
    private AudioSource audioSource;

    private bool canSpawn = false;
    // Start is called before the first frame update
    void Start()
    {
        gizmoManager = GetComponent<GizmoManager>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentObject != null) {
            //If attempting to spawn, place cube if it is in a valid space
            if (Input.GetMouseButtonUp(0)) {
                if (!canSpawn) {
                    Destroy(currentObject);
                    audioSource.PlayOneShot(failSound);
                }
                else {
                    audioSource.PlayOneShot(placeSound);
                }
                currentObject = null;
            }
            //Move cube
            else {
                Vector3 mousePoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10);
                Vector3 desiredPosition = Camera.main.ScreenToWorldPoint(mousePoint);

                currentObject.transform.position = desiredPosition;
            }
        }
    }

    //Create a cube at the mouse position, but disallow spawning until the user has moved away from the button
    public void SpawnClicked()
    {
        currentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentObject.transform.position = Camera.main.transform.position+Camera.main.transform.forward*10;
        canSpawn= false;
        audioSource.PlayOneShot(clickSound);
        gizmoManager.EnableGizmo(false);
    }

    public void AllowSpawn()
    {
        canSpawn= true;
        if (currentObject != null) {
            currentObject.SetActive(true);
        }
    }
}

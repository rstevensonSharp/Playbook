using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoManager : MonoBehaviour
{
    public GameObject gizmoPrefab;
    private Gizmo gizmo;
    // Start is called before the first frame update
    void Start()
    {
        gizmo = Instantiate(gizmoPrefab).GetComponent<Gizmo>();
        gizmo.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //Select an object if gizmo is inactive
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();
        if (Input.GetMouseButtonDown(0)) {
            bool hitObject = Physics.Raycast(ray,out hit,100);
            if (!gizmo.isActive && hitObject) {
                gizmo.SetTarget(hit.collider.transform);
            }
        }
        
    }

    public void EnableGizmo(bool state)
    {
        gizmo.SetActive(state);
    }
}

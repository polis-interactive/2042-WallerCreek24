using UnityEngine;
using static System.Collections.Specialized.BitVector32;

[RequireComponent(typeof(MeshCollider))]
public class BoundingVolumeFinal : MonoBehaviour
{

    private bool drawVolume = false;

    public void Setup(bool drawVolume = false) 
    {
        this.drawVolume = drawVolume;
        if (!drawVolume)
        {
            GetComponent<Renderer>().enabled = false;
        }
        else
        {
            GetComponent<Renderer>().enabled = true;
        }
    }

    public BoundingCollisionFinal CheckCollision(Vector3 position)
    {
        var boundingCollision = new BoundingCollisionFinal();
        var usePosition = position + 0.001f * Vector3.down;
        if (Physics.Raycast(usePosition, Vector3.down, out RaycastHit entryHit))
        {
            //var hitObject = entryHit.transform.gameObject;
            //var target = hitObject.GetComponent<BoundingVolumeFinal>();
            //if (!target)
            //{
            //    Debug.Log($"hit: {hitObject.name}");
            //}
            boundingCollision.doesCollide = true;
            boundingCollision.entryDistance = entryHit.distance + 0.001f;
            var entryPosition = entryHit.point + 0.001f * Vector3.down;
            if (Physics.Raycast(entryPosition, Vector3.down, out RaycastHit exitHit))
            {
                //hitObject = exitHit.transform.gameObject;
                //target = hitObject.GetComponent<BoundingVolumeFinal>();
                //if (!target)
                //{
                //    Debug.Log($"hit x2: {hitObject.name}");
                //}
                boundingCollision.hitTwice = true;
                boundingCollision.exitDistance = 0.001f + exitHit.distance;
            }
        }
        return boundingCollision;
    }
}

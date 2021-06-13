using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

public class SaveObject : MonoBehaviour, ISaveable {

    public enum SaveType { Transform, TransformRigidbody, Position, Rotation, RendererActive, ObjectActive }
    public SaveType saveType = SaveType.Transform;

    public Dictionary<string, object> OnSave()
    {
        if (saveType == SaveType.Transform)
        {
            return new Dictionary<string, object>
            {
                {"obj_enabled", GetComponent<MeshRenderer>().enabled},
                {"position", transform.position},
                {"angles", transform.eulerAngles}
            };
        }
        else if (saveType == SaveType.TransformRigidbody)
        {
            return new Dictionary<string, object>
            {
                {"obj_enabled",  GetComponent<MeshRenderer>().enabled},
                {"position", transform.position},
                {"angles", transform.eulerAngles},
                {"rigidbody_kinematic", GetComponent<Rigidbody>().isKinematic},
                {"rigidbody_gravity", GetComponent<Rigidbody>().useGravity},
                {"rigidbody_mass", GetComponent<Rigidbody>().mass},
                {"rigidbody_drag", GetComponent<Rigidbody>().drag},
                {"rigidbody_angdrag", GetComponent<Rigidbody>().angularDrag},
                {"rigidbody_freeze", GetComponent<Rigidbody>().freezeRotation},
                {"rigidbody_velocity", GetComponent<Rigidbody>().velocity},
            };
        }
        else if (saveType == SaveType.Position)
        {
            return new Dictionary<string, object>
            {
                {"position", transform.position }
            };
        }
        else if (saveType == SaveType.Rotation)
        {
            return new Dictionary<string, object>
            {
                {"angles", transform.eulerAngles }
            };
        }
        else if (saveType == SaveType.RendererActive)
        {
            return new Dictionary<string, object>
            {
                {"obj_enabled", GetComponent<MeshRenderer>().enabled}
            };
        }
        else if (saveType == SaveType.ObjectActive)
        {
            return new Dictionary<string, object>
            {
                {"obj_enabled", gameObject.activeSelf}
            };
        }

        return null;
    }

    public void OnLoad(JToken token)
    {
        if (token.HasValues)
        {
            if (saveType == SaveType.Transform)
            {
                DisableObject(gameObject, token["obj_enabled"].ToObject<bool>());
                transform.position = token["position"].ToObject<Vector3>();
                transform.eulerAngles = token["angles"].ToObject<Vector3>();
            }
            else if(saveType == SaveType.TransformRigidbody)
            {
                DisableObject(gameObject, token["obj_enabled"].ToObject<bool>());
                transform.position = token["position"].ToObject<Vector3>();
                transform.eulerAngles = token["angles"].ToObject<Vector3>();
                GetComponent<Rigidbody>().isKinematic = token["rigidbody_kinematic"].ToObject<bool>();
                GetComponent<Rigidbody>().useGravity = token["rigidbody_gravity"].ToObject<bool>();
                GetComponent<Rigidbody>().mass = token["rigidbody_mass"].ToObject<float>();
                GetComponent<Rigidbody>().drag = token["rigidbody_drag"].ToObject<float>();
                GetComponent<Rigidbody>().angularDrag = token["rigidbody_angdrag"].ToObject<float>();
                GetComponent<Rigidbody>().freezeRotation = token["rigidbody_freeze"].ToObject<bool>();
                GetComponent<Rigidbody>().velocity = token["rigidbody_velocity"].ToObject<Vector3>();
            }
            else if (saveType == SaveType.Position)
            {
                transform.position = token["position"].ToObject<Vector3>();
            }
            else if (saveType == SaveType.Rotation)
            {
                transform.eulerAngles = token["angles"].ToObject<Vector3>();
            }
            else if (saveType == SaveType.RendererActive)
            {
                DisableObject(gameObject, token["obj_enabled"].ToObject<bool>());
            }
            else if (saveType == SaveType.ObjectActive)
            {
                gameObject.SetActive(token["obj_enabled"].ToObject<bool>());
            }
        }
    }

    void DisableObject(GameObject obj, bool active)
    {
        if (active == false)
        {
            if (obj.GetComponent<InteractiveItem>())
            {
                obj.GetComponent<InteractiveItem>().DisableObject(active);
            }
            else
            {
                obj.GetComponent<MeshRenderer>().enabled = false;
                obj.GetComponent<Collider>().enabled = false;
            }
        }
    }
}
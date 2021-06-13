using UnityEngine;

public class Heal : MonoBehaviour {

	public float HealAmout;
	public AudioClip HealSound;
	
	private HealthManager hm;

	void Start()
	{
        hm = PlayerController.Instance.GetComponent<HealthManager>();

    }

    public void UseObject()
    {
        hm.ApplyHeal(HealAmout);
        if (!hm.isMaximum)
        {
            if (HealSound)
            {
                AudioSource.PlayClipAtPoint(HealSound, transform.position, 1.0f);
            }

            Destroy(gameObject);
        }
    }
}

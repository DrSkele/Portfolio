using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AgentHealthBar : MonoBehaviour {

    public Text HealthCount;

	public void SetHealthBar(int health)
    {
        HealthCount.text = "";
        for (int i = 0; i < health; i++)
        {
            HealthCount.text += "-";
        }
    }
}

using UnityEngine;
using TMPro;

public class TokenUIController : MonoBehaviour
{
    public TextMeshProUGUI GText, YText, RText;

    private int greenTokens = 0;
    private int yellowTokens = 0;
    private int redTokens = 0;

    void Start()
    {
        UpdateUI();
    }

    public void OnTokenCollected(string color)
    {
        switch (color)
        {
            case "Green":
                greenTokens++;
                break;
            case "Yellow":
                yellowTokens++;
                break;
            case "Red":
                redTokens++;
                break;
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        Debug.Log("Coin collected");
        GText.text = ": " + greenTokens;
        YText.text = ": " + yellowTokens;
        RText.text = ": " + redTokens;
    }
}


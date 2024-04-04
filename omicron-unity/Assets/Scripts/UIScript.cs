using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScript : MonoBehaviour
{
    public StarDataParser starDataParser;
    public Slider velocitySlider;
    public Slider distanceSlider;

    private float previousVelocityValue;
    private float previousDistanceValue;

    private void Start()
    {
        // Initialize previous slider values
        previousVelocityValue = velocitySlider.value;
        previousDistanceValue = distanceSlider.value;
    }

    private void Update()
    {
        // Check if the velocity slider value has changed
        if (velocitySlider.value != previousVelocityValue)
        {
            // If the slider value has changed, update the star velocities
            starDataParser.ChangeStarVelocities(velocitySlider.value);

            // Update previousVelocityValue
            previousVelocityValue = velocitySlider.value;
        }

        // Check if the distance slider value has changed
        if (distanceSlider.value != previousDistanceValue)
        {
            // If the slider value has changed, update the star distances
            starDataParser.ChangeStarDistances(distanceSlider.value);

            // Update previousDistanceValue
            previousDistanceValue = distanceSlider.value;
        }
    }
}

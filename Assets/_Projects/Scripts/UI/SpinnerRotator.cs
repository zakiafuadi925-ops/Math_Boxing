using UnityEngine;

public class SpinnerRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 200f; // Kecepatan rotasi dalam derajat per detik
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }
}

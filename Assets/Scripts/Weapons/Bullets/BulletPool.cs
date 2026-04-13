using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private GameObject basicBulletPrefab;
    [SerializeField] private int basicBulletPoolSize = 10;
    private List<GameObject> basicBulletList = new List<GameObject>();

    private static BulletPool instance;
    public static BulletPool Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        AddBasicBulletsToPool(basicBulletPoolSize);
    }

    private void AddBasicBulletsToPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject basicBullet = Instantiate(basicBulletPrefab);
            basicBullet.SetActive(false);
            basicBullet.transform.SetParent(transform);
            basicBulletList.Add(basicBullet);
        }
    }

    public GameObject RequestBasicBullet()
    {
        foreach (GameObject bullet in basicBulletList)
        {
            if (!bullet.activeSelf)
            {
                bullet.SetActive(true);
                return bullet;
            }
        }

        // Si no hay balas libres, podemos expandir el pool automáticamente
        GameObject newBullet = Instantiate(basicBulletPrefab);
        newBullet.SetActive(false);
        newBullet.transform.SetParent(transform);
        basicBulletList.Add(newBullet);
        newBullet.SetActive(true);
        return newBullet;
    }
}

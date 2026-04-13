using System.Collections.Generic;
using UnityEngine;

public class LevelProgressionManager : MonoBehaviour
{
    //Por si se me olvida como funciona. Simplemente por cada sala que haya en el mapa, añade a la lista level areas una nueva clase level area, ponle nombre, y luego cuando
    //el jugador colisione o interactué con una de las puertas, llama a la función de abajo para marcar ese área como desbloqueada.

    [System.Serializable]
    public class LevelArea
    {
        public string areaName;
        public List<GameObject> spawnPoints; // Spawnpoints de esta zona
        public bool unlocked;
    }

    public List<LevelArea> areas = new List<LevelArea>();

    /// <summary>
    /// Función encargada de activar los objetos SpawPoint para zombis
    /// </summary>
    /// <param name="areaName"> Nombre del área en STRING</param>
    public void UnlockArea(string areaName)
    {
        LevelArea area = areas.Find(a => a.areaName == areaName);

        if (area != null && !area.unlocked)
        {
            area.unlocked = true;
            foreach (var sp in area.spawnPoints)
            {
                sp.SetActive(true); // Activa los spawnpoints de esa zona
            }

            Debug.Log($"Área desbloqueada: {areaName} ({area.spawnPoints.Count} puntos de aparición activados)");
        }
    }

    //Cuando el jugador abra una puerta o alcance un punto de progreso, llama a:
    //FindAnyObjectByType<LevelProgressionManager>().UnlockArea("NombreDeArea");

}

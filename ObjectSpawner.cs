using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ObjectSpawner : MonoBehaviour
{
    public enum ObjectType {  SmallGem, BigGem, Enemy }

    public Tilemap tilemap;
    public GameObject[] objectPrefabs; // 0 = SmallGem, BigGem, Enemy
    public float bigGemProbibility = 0.2f;
    public float enemyProbibility = 0.1f;
    public int maxObject = 5;
    public float gemLifeTime = 10f;
    public float spawnInterval = 0.5f;

    private List<Vector3> validSpawnPositions= new List<Vector3>(); 
    private List<GameObject> spawnObjects = new List<GameObject>();
    private bool isSpawning = false;

    void Start()
    {
        GatherValidPositions();
        StartCoroutine(SpawnObjectsIfNeeded());
        GameController.OnReset += LevelChange;
    }

    // Update is called once per frame
    void Update()
    {
        if (!tilemap.gameObject.activeInHierarchy)
        {
            LevelChange();
        }

        if(!isSpawning && ActiveObjectCount() < maxObject)
        {
            StartCoroutine(SpawnObjectsIfNeeded()); 
        }
    }

    private void LevelChange()
    {
        tilemap = GameObject.Find("Ground").GetComponent<Tilemap>();
        GatherValidPositions();
        DestroyAllSpawnedObjects();
    }

    private int ActiveObjectCount()
    {
        spawnObjects.RemoveAll(item => item == null);
        return spawnObjects.Count;
    }

    private IEnumerator SpawnObjectsIfNeeded()
    {
        isSpawning = true;
        while(ActiveObjectCount() < maxObject)
        {
            SpawnObject();
            yield return new WaitForSeconds(spawnInterval);
        }
        isSpawning = false;
    }

    private bool PositionHasObject(Vector3 posToCheck)
    {
        return spawnObjects.Any(checkObj => checkObj && Vector3.Distance(checkObj.transform.position, posToCheck) < 1.0f);  // If there's already object exsisting, return true
    }

    private ObjectType RandomObjType()
    {
        float randomChoice = Random.value; // 0~1

        if(randomChoice <= enemyProbibility)
        {
            return ObjectType.Enemy;
        }
        else if(randomChoice <= (enemyProbibility + bigGemProbibility))
        {
            return ObjectType.BigGem;
        }
        else
        {
            return ObjectType.SmallGem;
        }
    }

    private void SpawnObject()
    {
        if (validSpawnPositions.Count == 0) { return; }

        Vector3 spawnPos = Vector3.zero;
        bool validPostionFound = false;

        while(!validPostionFound && validSpawnPositions.Count > 0)
        {
            int randomIndex = Random.Range(0, validSpawnPositions.Count);
            Vector3 potentialPos = validSpawnPositions[randomIndex];
            Vector3 leftPos = potentialPos + Vector3.left;   // Check both sides
            Vector3 rightPos = potentialPos + Vector3.right;

            if(!PositionHasObject(leftPos) && !PositionHasObject(rightPos))
            {
                spawnPos = potentialPos;
                validPostionFound =true;
            }

            validSpawnPositions.RemoveAt(randomIndex);  
        }

        if (validPostionFound)
        {
            ObjectType objectType = RandomObjType();
            GameObject gameObject = Instantiate(objectPrefabs[(int) objectType], spawnPos, Quaternion.identity);
            spawnObjects.Add(gameObject);

            //Destroy Gems only after time
            if(objectType != ObjectType.Enemy)
            {
                StartCoroutine(DestroyObjAfterTime(gameObject, gemLifeTime));
            }
        }
    }

    private IEnumerator DestroyObjAfterTime(GameObject gameObject, float time)
    {
        yield return new WaitForSeconds(time);

        if (gameObject)
        {
            spawnObjects.Remove(gameObject);
            validSpawnPositions.Add(gameObject.transform.position);
            Destroy(gameObject);
        }
    }

    private void DestroyAllSpawnedObjects()
    {
        foreach(GameObject obj in spawnObjects)
        {
            if(obj != null)
            {
                Destroy(obj);
            }
        }
        spawnObjects.Clear();
    }


    private void GatherValidPositions()
    {
        validSpawnPositions.Clear();
        BoundsInt boundsInt = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(boundsInt);
        Vector3 start = tilemap.CellToWorld(new Vector3Int(boundsInt.xMin, boundsInt.yMin, 0));

        for(int x = 0; x < boundsInt.size.x; x++)
        {
            for(int y = 0; y < boundsInt.size.y; y++)
            {
                TileBase tile = allTiles[x + y * boundsInt.size.x];
                if(tile != null)
                {
                    Vector3 place = start + new Vector3(x + 0.5f, y + 1.5f, 0);
                    validSpawnPositions.Add(place);
                }
            }
        }
    }


}

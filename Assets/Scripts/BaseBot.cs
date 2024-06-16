using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Scanner), typeof(Spawner))]
public class BaseBot : MonoBehaviour
{
    private float _resourceCollectionDelay = 0.1f;
    private float _spawnRadius = 3f;
    private int _resourcesForNewBase = 5;
    private int _resourcesForNewBot = 3;
    private int _resourceCount = 0;
    private int _startCountBots = 3;

    private bool _isFlagPlaced = false;
    private bool _isCreatedUnit = false;

    private List<Unit> _bots = new List<Unit>();

    private Spawner _createBot;
    private Scanner _scanner;
    private Flag _flag;
    private ResourceData _resourceData;

    public event UnityAction<int> ResourcesChanged;

    private void Awake()
    {
        _scanner = GetComponent<Scanner>();
        _createBot = GetComponent<Spawner>();
        _resourceData = FindAnyObjectByType<ResourceData>();
    }

    private void Start()
    {
        if (_isCreatedUnit == false)
        {
            CreateBot(_startCountBots);
        }

        StartCoroutine(CollectResourcesRoutine());
    }

    public void SetFlagPlaced()
    {
        _isFlagPlaced = false;
    }

    public void AddBot(Unit bot)
    {
        if (bot != null && _bots.Contains(bot) == false)
        {
            _bots.Add(bot);
        }
    }

    public void SetUnitCreated()
    {
        _isCreatedUnit = true;
    }

    public void SetFlag(Flag flag)
    {
        _flag = flag;
        _isFlagPlaced = true;
    }

    public void TakeResource(Resource resource)
    {

        if (_isFlagPlaced)
        {
            if (_resourceCount >= _resourcesForNewBase)
            {
                SpawnNewBase();
            }
        }
        else
        {
            int countNewBot = _resourceCount / _resourcesForNewBot;

            CeateNewBot(countNewBot);
        }

        _resourceCount++;
        ResourcesChanged?.Invoke(_resourceCount);

        _resourceData.ReleaseResource(resource);
    }

    public void RemoveFlag()
    {
        _isFlagPlaced = false;
        Destroy(_flag.gameObject);
        _flag = null;
    }

    public void DetachUnit(Unit unit)
    {
        if (unit != null && _bots.Contains(unit))
        {
            _bots.Remove(unit);
            unit.SetBaseBot(null);
            unit.IsBusy = false;
        }
    }

    private void SpawnNewBase()
    {
        foreach (Unit bot in _bots)
        {
            if (bot.IsBusy == false)
            {
                bot.SetDestination(_flag);
                _resourceCount -= _resourcesForNewBase;
                break;
            }
        }
    }

    private void CeateNewBot(int countNewBot)
    {
        if (_resourceCount >= _resourcesForNewBot)
        {
            for (int i = 0; i < countNewBot; i++)
            {
                _resourceCount -= _resourcesForNewBot;
                CreateBot(1);
            }
        }
    }

    private void CreateBot(int startCount)
    {
        for (int i = 0; i < startCount; i++)
        {
            float randomX = Random.Range(-_spawnRadius, _spawnRadius);
            float randomZ = Random.Range(-_spawnRadius, _spawnRadius);
            Vector3 randomPosition = transform.position + new Vector3(randomX, 0, randomZ);
            Unit bot = _createBot.Spawn(randomPosition);
            bot.SetBaseBot(this);
            _bots.Add(bot);
        }
    }

    private IEnumerator CollectResourcesRoutine()
    {
        var waitSeconds = new WaitForSeconds(_resourceCollectionDelay);

        while (true)
        {
            yield return waitSeconds;
            CollectResource();
        }
    }

    private void CollectResource()
    {
        if (_isFlagPlaced && _resourceCount >= _resourcesForNewBase)
        {
            foreach (Unit bot in _bots)
            {
                if (!bot.IsBusy)
                {
                    bot.SetDestination(_flag); 
                    _resourceCount -= _resourcesForNewBase; 
                    ResourcesChanged?.Invoke(_resourceCount);
                    break; 
                }
            }
        }
        else
        {
            List<Resource> availableResources = _scanner.GetAllResources()
                .Where(resource => !_resourceData.IsResourceBusy(resource))
                .ToList();

            if (availableResources.Count > 0)
            {
                Resource resource = availableResources.First();

                foreach (Unit bot in _bots)
                {
                    if (bot.IsBusy == false)
                    {
                        _resourceData.OccupyResource(resource);
                        bot.SetDestination(resource);
                        break;
                    }
                }
            }
        }
    }
}

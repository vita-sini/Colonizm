using System;
using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private BaseBot _baseBotPrefab;

    private float _moveSpeed = 15f;
    private float _pickupRange = 0.5f;
    private float _carryDistance = 0.5f;
    private float _buildDistance = 1f;

    private BaseBot _baseBot;
    private Resource _carriedResource;
    private Flag _targetFlag;

    public bool IsBusy { get;  set; } = false;

    public void SetDestination(Component targetComponent)
    {
        if (targetComponent == null)
            return;

        IsBusy = true;

        if (targetComponent is Resource resource)
        {
            _carriedResource = resource;
            StartCoroutine(MoveToTarget(resource.transform, _pickupRange, PickupResource));
        }
        else if (targetComponent is Flag flag)
        {
            _targetFlag = flag;
            StartCoroutine(MoveToTarget(flag.transform, _buildDistance, CreateNewBase));
        }
    }

    public void SetBaseBot(BaseBot baseBot)
    {
        _baseBot = baseBot;
    }

    private void CreateNewBase()
    {
        _baseBot.RemoveFlag();
        Vector3 newBasePosition = new Vector3(_targetFlag.transform.position.x, 1.01f, _targetFlag.transform.position.z);
        BaseBot newBase = Instantiate(_baseBotPrefab, newBasePosition, Quaternion.identity);
        newBase.SetFlag(_targetFlag);
        newBase.SetUnitCreated();
        newBase.SetFlagPlaced();
        _baseBot.DetachUnit(this);

        _baseBot = newBase;
        newBase.AddBot(this);

        _targetFlag = null;
    }

    private void PickupResource()
    {
        if (_carriedResource == null)
            return;

        _carriedResource.transform.SetParent(transform);
        _carriedResource.transform.localPosition = Vector3.forward * _carryDistance;
        _carriedResource.transform.localRotation = Quaternion.identity;

        StartCoroutine(MoveToTarget(_baseBot.transform, _carryDistance, DropResource));
    }

    private void DropResource()
    {
        if (_carriedResource == null || _baseBot == null)
            return;

        _carriedResource.transform.SetParent(null);
        _baseBot.TakeResource(_carriedResource);
        _carriedResource.Release();
        _carriedResource = null;

        IsBusy = false;
    }
    private IEnumerator MoveToTarget(Transform target, float stopDistance, Action onComplete)
    {
        while ((target.position - transform.position).sqrMagnitude > stopDistance * stopDistance)
        {
            transform.position += (target.position - transform.position).normalized * _moveSpeed * Time.deltaTime;
            yield return null;
        }

        onComplete();
    }
}
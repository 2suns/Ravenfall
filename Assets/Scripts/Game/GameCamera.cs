﻿using System;
using FlatKit;
using UnityEngine;

public class GameCamera : MonoBehaviour
{
    private const float ObserverJumpTimer = 10f;

    [SerializeField] private FreeCamera freeCamera;
    [SerializeField] private MouseOrbitCamera orbitCamera;
    [SerializeField] private FocusTargetCamera focusTargetCamera;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RaidManager raidManager;
    [SerializeField] private PlayerObserveCamera observeCamera;
    [SerializeField] private GameObject arena;

    [SerializeField] private PlayerDetails playerObserver;

    private float observeNextPlayerTimer = ObserverJumpTimer;
    private int observedPlayerIndex;

    private GameCameraType state = GameCameraType.Free;

    public bool AllowJoinObserve { get; private set; } = true;

    // Start is called before the first frame updateF
    void Start()
    {
        if (!freeCamera) freeCamera = GetComponent<FreeCamera>();
        if (!orbitCamera) orbitCamera = GetComponent<MouseOrbitCamera>();
        if (!focusTargetCamera) focusTargetCamera = GetComponent<FocusTargetCamera>();

        if (!playerObserver) playerObserver = gameManager.ObservedPlayerDetails;
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager == null ||
            gameManager.RavenNest == null ||
            !gameManager.RavenNest.Authenticated ||
            !gameManager.IsLoaded)
        {
            return;
        }

        if (state != GameCameraType.Free &&
            Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Tab))
        {
            state = GameCameraType.Free;
            playerObserver.Observe(null, 0);
            observeCamera.ObservePlayer(null);
            orbitCamera.targetTransform = null;
            freeCamera.enabled = true;
            orbitCamera.enabled = false;
            focusTargetCamera.enabled = false;

            AllowJoinObserve = false;

            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (ObserveNextPlayer()) return;
            return;
        }

        if (state == GameCameraType.Arena)
        {
            freeCamera.enabled = false;
            orbitCamera.enabled = false;
            focusTargetCamera.enabled = true;
            if (arena) focusTargetCamera.Target = arena.transform;
        }
        else if (state == GameCameraType.Raid)
        {
            freeCamera.enabled = false;
            orbitCamera.enabled = false;
            focusTargetCamera.enabled = true;
            if (arena) focusTargetCamera.Target = raidManager.Boss.transform;
        }
        else if (state == GameCameraType.Observe)
        {
            AllowJoinObserve = true;
            observeNextPlayerTimer -= Time.deltaTime;
            if (observeNextPlayerTimer <= 0)
            {
                ObserveNextPlayer();
            }
        }
    }

    public bool ObserveNextPlayer()
    {
        var playerCount = playerManager.GetPlayerCount(true);
        if (playerCount == 0)
        {
            return true;
        }

        state = GameCameraType.Observe;
        focusTargetCamera.enabled = false;
        observedPlayerIndex = (observedPlayerIndex + 1) % playerCount;
        var player = playerManager.GetPlayerByIndex(observedPlayerIndex);
        //var player = this.playerManager.GetRandomPlayer(this.orbitCamera.targetTransform);
        if (!player)
        {
            return true;
        }

        ObservePlayer(player);
        return false;
    }

    public void EnableRaidCamera()
    {
        state = GameCameraType.Raid;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        orbitCamera.targetTransform = null;
    }

    public void EnableArenaCamera()
    {
        state = GameCameraType.Arena;
        observeCamera.ObservePlayer(null);
        playerObserver.Observe(null, 0);
        orbitCamera.targetTransform = null;
    }

    public void DisableFocusCamera()
    {
        if (state != GameCameraType.Observe)
            ObserveNextPlayer();
    }

    public void ObservePlayer(PlayerController player)
    {
        var subMultiplier = player.IsSubscriber ? 2f : 1f;
        ObservePlayer(player, ObserverJumpTimer * subMultiplier);
    }

    public void ObservePlayer(PlayerController player, float time)
    {
        observeNextPlayerTimer = time;
        observeCamera.ObservePlayer(player);
        playerObserver.Observe(player, time);
        freeCamera.enabled = false;
        orbitCamera.targetTransform = player.transform;
        orbitCamera.enabled = true;
    }
}

public enum GameCameraType
{
    Free,
    Observe,
    Arena,
    Raid
}

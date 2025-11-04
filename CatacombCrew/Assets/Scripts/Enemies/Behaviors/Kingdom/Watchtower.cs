    using UnityEngine;
    using System.Collections.Generic;

    public class Watchtower : EnemyBase
    {

        /*  The watchtower is a static enemy that uses a spotlight and collider to scan an area and look for the players
            if the players are spotted by the collider inside of the spotlight, the watchtower alerts the other enemies in the level
            and feeds them the player loceation for 10 seconds; no listening
        */

        public GameObject spotLight;
        public List<Transform> lightPoints = new List<Transform>();
        public Transform[] guards;

        private float rotateSpeed = 75;
        private float stayOnPoint = 1.25f;
        private float alertSeconds = 10f;
        private float alertInterval = 0.5f;

        private Transform playerLock;
        public Collider spotTrigger;
        private Vector3 lastSeenPos;
        private int playerLayer;
        private int pointDest;
        private float focusUntil;
        private float nextPointAt;
        private float alertUntil;
        private float nextPingAt;

        private float spotRange = 15f;
        private bool alerting;
        private bool focused;

        Vector3 triggerInitialLocalScale;
        public LayerMask groundMask = ~0;


        public override void Awake(){
            base.Awake();

            if(eyes == null) eyes = transform;
            if(spotTrigger != null) triggerInitialLocalScale = spotTrigger.transform.localScale;
            sightDistance = 0f;
            playerLayer = LayerMask.NameToLayer("Player");
      
        }

        public  void Start(){
            if(lightPoints.Count > 0){
                pointDest = NearestPoint();
                LookAt(lightPoints[pointDest].position);
                focusUntil = Time.time + stayOnPoint;
            }
     
        }

        public override void Update(){
            if(focused && playerLock != null){
                lastSeenPos = playerLock.position;
                LookAt(lastSeenPos);
            }
            else{
                pointLight();
            }

            if(Time.time <= alertUntil){
                if(Time.time >= nextPingAt){
                    nextPingAt = Time.time + alertInterval;
                    AlertGuards(lastSeenPos);
                }
            }

            BindColliderToGround();

        }

        public void LateUpdate(){
            if(spotTrigger != null)
                spotTrigger.transform.localScale = triggerInitialLocalScale;
        }

        public void pointLight(){
            if(lightPoints.Count == 0) return;
            if(Time.time < focusUntil) return;

            Vector3 point = lightPoints[pointDest].position;
            if(RotateTowards(point)){
                pointDest = (pointDest + 1) % lightPoints.Count;
                focusUntil = Time.time + stayOnPoint;
            }

        }

        void OnTriggerEnter(Collider other){
            if(other.gameObject.layer != playerLayer) return;
            var rigid = other.attachedRigidbody;
            playerLock = rigid ? rigid.transform : other.transform;
            lastSeenPos = other.bounds.center;
            focused = true;
            BeginAlert();
        }

        void OnTriggerStay(Collider other){
            if(other.gameObject.layer != playerLayer) return;
            if(playerLock == null){
                var rigid = other.attachedRigidbody;
                playerLock = rigid ? rigid.transform : other.transform;
            }
            lastSeenPos = other.bounds.center;
            focused = true;
            alertUntil = Time.time + alertSeconds;
        }

        void OnTriggerExit(Collider other){
            if(other.gameObject.layer != playerLayer) return;
            var trig = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            if(trig == playerLock){
                playerLock = null;
                focused = false;
            }
        }

        void BeginAlert(){
            alertUntil = Time.time + alertSeconds;
            nextPingAt = 0f;
            AlertGuards(lastSeenPos);
        }

        void AlertGuards(Vector3 playerPosition){
            if(guards == null) return;
            for(int i = 0; i < guards.Length; i++){
                var t = guards[i];
                var guard = t.GetComponent<EnemyBase>();
                if(guard != null){
                    guard.OnSound(playerPosition, Vector3.zero, 1f, gameObject);
                }
            }
        }

        bool RotateTowards(Vector3 worldPoint){
            Vector3 direction = worldPoint - eyes.position;
            if(direction.sqrMagnitude < 0.0001f) return true;
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            eyes.rotation = Quaternion.RotateTowards(eyes.rotation, targetRot, rotateSpeed * Time.deltaTime);
            if(spotLight != null && spotLight.transform != eyes){
                spotLight.transform.rotation = eyes.rotation;
            }
            float angle = Quaternion.Angle(eyes.rotation, targetRot);
            return angle < 2f;
        }
      
        void LookAt(Vector3 worldPoint){
            Vector3 direction = worldPoint - eyes.position;
            if(direction.sqrMagnitude < 0.0001f) return;
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            eyes.rotation = targetRot;
            if(spotLight != null && spotLight.transform != eyes){
                spotLight.transform.rotation = eyes.rotation;
            }
        }

        int NearestPoint(){
            if(lightPoints == null || lightPoints.Count == 0) return 0;
            int best = 0;
            float bestD2 = float.PositiveInfinity;
            Vector3 pos = eyes.position;
            for(int i = 0; i < lightPoints.Count; i++){
                var t = lightPoints[i];
                if(t == null) continue;
                float d2 = (t.position - pos).sqrMagnitude;
                if(d2 < bestD2){
                    bestD2 = d2;
                    best = i;
                }
            }
            return best;
        }

        void BindColliderToGround(){
            if(spotTrigger == null) return;
            var trig = spotTrigger.transform;

            if(Physics.Raycast(eyes.position, eyes.forward, out var hit, spotRange, groundMask, QueryTriggerInteraction.Ignore)){
                trig.position = hit.point + Vector3.up * 0.02f;
            }
            else{
                trig.position = eyes.position + eyes.forward * spotRange;
                trig.position = new Vector3(trig.position.x, eyes.position.y - 0.01f, trig.position.z);
            }

            float yaw = eyes.eulerAngles.y;
            trig.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

    }
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

        // Skull Object
        public Transform skull;

        private float rotateSpeed = 75;
        private float stayOnPoint = 1.25f;
        private float alertSeconds = 10f;
        private float alertInterval = 0.5f;

        private float beamRadius = 0.75f;
        private float LOSgrace = 0.5f;
        private float LOSDropAt;

        private Transform playerLock;
        public Collider spotTrigger;
        Transform beamPivot;
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

        public LayerMask groundMask;

        public override void Awake(){
            base.Awake();

            if(eyes == null) eyes = transform;
            sightDistance = 0f;
            groundMask = LayerMask.NameToLayer("Ground");
            playerLayer = LayerMask.NameToLayer("Player");

            beamPivot = (spotLight != null) ? spotLight.transform : eyes;

            if(spotTrigger != null){
                spotTrigger.transform.SetParent(eyes, worldPositionStays: false);
                spotTrigger.transform.localPosition = Vector3.zero;
                spotTrigger.transform.localRotation = Quaternion.identity;
                spotTrigger.transform.localScale = Vector3.one;
                spotTrigger.isTrigger = true;
            }

            var rigid = beamPivot.GetComponent<Rigidbody>();
            if(rigid == null){
                rigid = beamPivot.gameObject.AddComponent<Rigidbody>();
                rigid.isKinematic = true;
                rigid.useGravity = false;
            }
      
            if(spotTrigger is BoxCollider box){
                box.size = new Vector3(beamRadius * 2f, beamRadius * 2f, box.size.z);
            }

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

                Vector3 from = beamPivot.position;
                Vector3 to = lastSeenPos + Vector3.up * 0.9f;
                if(HasLos(from, to)){
                    LOSDropAt = Time.time + LOSgrace;
                }
                else if(Time.time >= LOSDropAt){
                    playerLock = null;
                    focused = false;
                }

            }
            else{
                pointLight();
            }

            if(Time.time <= alertUntil && Time.time >= nextPingAt){
                if(Time.time >= nextPingAt){
                    nextPingAt = Time.time + alertInterval;
                    AlertGuards(lastSeenPos);
                }
            }

            UpdateBeamTrigger();

            // Makes the skull rotate with spotlight
            if (skull != null && spotLight != null)
            {
                skull.rotation = spotLight.transform.rotation;
                skull.localPosition = new Vector3(0, Mathf.Sin(Time.time * 2f) * 0.05f, 0);
            }

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

            if(HasLos(eyes.position, lastSeenPos + Vector3.up * 0.9f)){
                LOSDropAt = Time.time + LOSgrace;
            }
        }

        void OnTriggerExit(Collider other){
            if(other.gameObject.layer != playerLayer) return;
            var trig = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            if(trig == playerLock){

                if(HasLos(beamPivot.position, trig.position + Vector3.up * 0.9f)){
                    LOSDropAt = Time.time + LOSgrace;
                }
                else{
                    playerLock = null;
                    focused = false;
                }
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

        bool HasLos(Vector3 from, Vector3 to){
            return !Physics.Linecast(from, to, obstructionMask, QueryTriggerInteraction.Ignore);
        }

        void UpdateBeamTrigger(){
            if(spotTrigger == null) return;

            Vector3 origin = beamPivot.position + beamPivot.forward * 0.05f;
            Vector3 direction = beamPivot.forward;

            if(!Physics.Raycast(origin, direction, out var hit, spotRange, groundMask, QueryTriggerInteraction.Ignore)){
                hit.point = origin + direction * spotRange;
                hit.distance = spotRange;
            }

            float len = Mathf.Max(0.1f, hit.distance - 0.02f);

            if(spotTrigger is BoxCollider box){
                box.size = new Vector3(beamRadius * 2f, beamRadius * 2f, len);
                box.center = new Vector3(0f, 0f, len * 0.5f);
            }

        }

    }
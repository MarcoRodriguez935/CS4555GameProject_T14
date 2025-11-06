    using UnityEngine;
    using System.Collections.Generic;

    public class Watchtower : EnemyBase
    {

        /*  The watchtower is a static enemy that uses a spotlight and collider to scan an area and look for the players
            if the players are spotted by the collider inside of the spotlight, the watchtower alerts the other enemies in the level
            and feeds them the player loceation for 10 seconds; no listening
        */

        //TWEAK THE RANGE OF THE LIGHT
            //range of the collider will be scaled down by 0.67, 
                //try to make it hit the floor or go slightly under it; not more.
        //MAKE SURE LOOKPOINTS ARE WITHIN RANGE

        public Transform pivot;
        public GameObject spotLight;
        public List<Transform> lightPoints = new List<Transform>();
        public Transform[] guards;

        // Skull Object
        public Transform skull;

        private float rotateSpeed = 45f;
        private float stayOnPoint = 1.25f;
        private float alertSeconds = 10f;
        private float alertInterval = 0.5f;

        private float beamWidth = 4f;
        private float beamHeight = .75f;
        private bool syncRange = true;
        private float LOSgrace = 0.75f;
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

        private float spotRange = 8f;
        private bool alerting;
        private bool focused;

        private Light spot;
        public LayerMask groundMask;
        public LayerMask beamHitMask;

        public override void Awake(){
            base.Awake();

            if(eyes == null) eyes = transform;
            sightDistance = 0f;
            groundMask = LayerMask.GetMask("Ground");
            beamHitMask = LayerMask.GetMask("Ground", "Wall", "Obstacle");
            playerLayer = LayerMask.NameToLayer("Player");

            beamPivot = (pivot != null) ? pivot : (spotLight ? spotLight.transform : eyes);

            if(spotTrigger != null){
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
      
            spot = spotLight ? spotLight.GetComponent<Light>() : null;
            if(syncRange && spot) spotRange = spot.range * 0.67f;

            if(spotTrigger is BoxCollider box){
                box.isTrigger = true;
                box.size = new Vector3(beamWidth, beamHeight, 0.1f);
                box.center = new Vector3(0f, 0f, 0.25f);
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
                nextPingAt = Time.time + alertInterval;
                AlertGuards(lastSeenPos);
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

            if(HasLos(beamPivot.position, lastSeenPos + Vector3.up * 0.9f)){
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

            Vector3 anchorPos = (pivot != null) ? pivot.position : (spotLight ? spotLight.transform.position : eyes.position);
            
            Vector3 forward = (spotLight ? spotLight.transform.forward : eyes.forward);
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

            Transform t = spotTrigger.transform;
            t.SetPositionAndRotation(anchorPos, rot);

            float offset = 0.25f;
            Vector3 origin = anchorPos + forward * offset;

            RaycastHit hit;
            if(!Physics.Raycast(origin, forward, out hit, spotRange, beamHitMask, QueryTriggerInteraction.Ignore)){
                hit.distance = spotRange;
            }

            float len = Mathf.Max(0.1f, hit.distance);

            var box = spotTrigger as BoxCollider;
            if(box){
                box.size = new Vector3(beamWidth, beamHeight, len);
                box.center = new Vector3(0f, beamHeight * 1.5f, len * 0.5f);
            }

        }

    }
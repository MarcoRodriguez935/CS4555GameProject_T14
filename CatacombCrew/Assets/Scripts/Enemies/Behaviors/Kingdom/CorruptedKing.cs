using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class CorruptedKing : EnemyBase
{

    /*  The CorruptedKing is an enemy that has been corrupted by dark forces. He sits on his throne until
        the players enter the radius of his 'powers', after which he will get up and become active. 
            If the players can be seen by him and are within a certain range, he will use a ranged spike attack
            If the players are close, he will swing his sword at them in a wide arc
            If the players are outside of his active range (the range should be around his throne object, not him),
                then the king will pace around the inside of the range for a while
            If the players are quiet and not seen inside of the range of the king, he will not become active;
            if the players are heard outside of the range, the king will call close guards and summon new ones
                the king can have a maximum of 4 summoned guards; 4 other guards will be placed before the level start
            if there is no stimuli for about 30 seconds, the king will sit down again and 'meditate'
    */

        public Transform throne;
        private float activeRadius = 15f; //radius around throne that king will become active in
        private float activePaceInner = 6f;
        private float activePaceOuter = 10f;
        private float meleeRange = 3.0f;
        private float rangedRange = 10f;
        private float meleeCooldown = 2.2f;
        private float rangedCooldown = 3.0f;
        private float meditateAfter = 30f; //no stimuli, become inactive again

        public Transform[] placedGuards;
        public GameObject guardPrefab;
        public Transform[] summonSpots;

        enum KingState { Seated, Active, Meditate }
        KingState state = KingState.Seated;

        private float nextMeleeAt;
        private float nextRangedAt;
        private float interestUntil;
        private int summonedCount;

        private Transform playerLock;
        private Rigidbody playerBody;
        private Vector3 lastKnownPos;

        public override void Awake(){
            base.Awake();

            agent.GetComponent<NavMeshAgent>();
            agent.updateRotation = true;
            agent.stoppingDistance = 0.5f;
            if(eyes == null) eyes = transform;
            if(throne == null) throne = transform;
            sightDistance = 22f;
        }

        public void Start(){
            SitOnThrone();
        }

        public override void Update(){
            base.Update();

            if(state == KingState.Seated) return;

            if(Time.time > interestUntil && state != KingState.Meditate){
                Meditate();
                return;
            }

            if(state == KingState.Meditate){
                if(Vector3.SqrMagnitude(transform.position - throne.position) <= 0.4f * 0.4f){
                    SitOnThrone();
                }
                return;
            }

            if(playerLock != null){
                lastKnownPos = playerLock.position;
                float distance = Vector3.Distance(transform.position, playerLock.position);

                if(distance <= meleeRange){
                    MeleeAttack();
                }
                else if(distance <= rangedRange){
                    RangedAttack();
                    Face(lastKnownPos);
                }
                else{
                    PaceInsideRing();
                }
            }

            else{
                PaceInsideRing();
            }

        }

        void MeleeAttack(){
            if(Time.time < nextMeleeAt) return;
            Debug.Log("MeleeAttack");
            nextMeleeAt = Time.time + meleeCooldown;

            agent.isStopped = true;
            //swinging sword animation
            //attack colliders and damage
            agent.isStopped = false;
        }

        void RangedAttack(){
            
            if(Time.time < nextRangedAt) return;
            Debug.Log("Ranged Attack");
            nextRangedAt = Time.time + rangedCooldown;

            agent.isStopped = true;
            //animation - spikes come up from floor staggered to last player location
            //colliders for player damage
            agent.isStopped = false;
        }

        void PaceInsideRing(){
            if(!agent.hasPath || agent.remainingDistance <= 0.6f){
                float radius = Random.Range(activePaceInner, activePaceOuter);
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 target = throne.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

                NavMeshHit hit;
                if(NavMesh.SamplePosition(target, out hit, 2.0f, NavMesh.AllAreas)){
                    agent.isStopped = true;
                    agent.speed = 2.4f;
                    agent.SetDestination(hit.position);
                }
            }
        }

        void Face(Vector3 point){
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if(direction.sqrMagnitude < 0.0001f) return;
            Quaternion q = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, q, 180f * Time.deltaTime);
        }

        void SitOnThrone(){
            state = KingState.Seated;
            agent.isStopped = false;
            agent.speed = 0f;
            agent.SetDestination(throne.position);
            //sitting animation
            playerLock = null;
            playerBody = null;
            summonedCount = Mathf.Clamp(summonedCount, 0, 4);
        }

        void Meditate(){
            state = KingState.Meditate;
            playerLock = null;
            playerBody = null;
            agent.isStopped = false;
            agent.speed = 2.0f;
            agent.SetDestination(throne.position);
            //meditation animation
        }

        void Activate(Transform player, Rigidbody body){
            state = KingState.Active;
            playerLock = player;
            playerBody = body;
            interestUntil = Time.time + meditateAfter;
            agent.isStopped = false;
            agent.speed = 2.4f;
        }

        void CallGuards(Vector3 where){
            if(placedGuards != null){
                for(int i = 0; i < placedGuards.Length; i++){
                    var guard = placedGuards[i] ? placedGuards[i].GetComponent<EnemyBase>() : null;
                    if(guard != null){
                        guard.OnSound(where, Vector3.zero, 1, gameObject);
                    }
                }
            }
            if(guardPrefab != null && summonSpots != null){
                for(int i = 0; i < summonSpots.Length && summonedCount < 4; i++){
                    var spot = summonSpots[i];
                    if(spot == null) continue;
                    var go = GameObject.Instantiate(guardPrefab, spot.position, spot.rotation);
                    summonedCount++;
                    var guard = go.GetComponent<EnemyBase>();
                    if(guard != null){
                        guard.OnSound(where, Vector3.zero, 1f, gameObject);
                    }
                }
            }
        }

        public override void OnSeen(Vector3 origin, Rigidbody player){
            base.OnSeen(origin, player);
            if(Vector3.Distance(throne.position, origin) <= activeRadius){
                if(state == KingState.Seated){
                    Activate(player ? player.transform : null, player);
                }
                else{
                    playerLock = player ? player.transform : playerLock;
                    playerBody = player ? player : playerBody;
                    interestUntil = Time.time + meditateAfter;
                }
                lastKnownPos = origin;
            }
        }

        public override void OnSound(Vector3 origin, Vector3 direction, float magnitude, GameObject reason){
            base.OnSound(origin, direction, magnitude, reason);
            float dToThrone = Vector3.Distance(throne.position, origin);
            if(dToThrone > activeRadius){
                CallGuards(origin);
                return;
            }
            if(state != KingState.Seated){
                interestUntil = Time.time + meditateAfter;
                lastKnownPos = origin;
            }
        }
}
// using UnityEngine;
// using System.Collections;
// using Unity.Netcode;
// using UnityEngine.XR.Interaction.Toolkit.Interactables;

// public class NetworkDart : NetworkBehaviour
// {
//     private Rigidbody _DartRigidbody;
//     private bool _IsFlying = false;
//     public float MaxLifetime = 10.0f;

//     private Coroutine _ReturnCoroutine;
//     private Coroutine _MaxLifetimeCoroutine;

//     private Transform _VisualTargetBone;
//     private bool _IsVisuallyAttached = false;

//     private void Awake()
//     {
//         _DartRigidbody = GetComponent<Rigidbody>();
//     }

//     private void LateUpdate()
//     {
//         // 시각적 부착 상태일 때만 실행
//         if (_IsVisuallyAttached && _VisualTargetBone != null)
//         {
//             // 1. 타겟(손 뼈)의 월드 좌표에 오프셋을 적용해 내 위치로 설정
//             // (ParentConstraint와 동일한 원리)
//             transform.position = _VisualTargetBone.position;
//             transform.rotation = _VisualTargetBone.rotation;
//         }
//     }

//     public override void OnNetworkSpawn()
//     {
//         _IsFlying = false;
//         _DartRigidbody.isKinematic = true;
//         _DartRigidbody.useGravity = false;
//         _DartRigidbody.linearVelocity = Vector3.zero;
//         _DartRigidbody.angularVelocity = Vector3.zero;
//     }

//     // 부모가 바뀔 때 호출되는 콜백 오버라이드
//     public override void OnNetworkObjectParentChanged(NetworkObject parentNetworkObject)
//     {
//         base.OnNetworkObjectParentChanged(parentNetworkObject);
//         if(parentNetworkObject != null)
//         {
//             // 부모가 설정되었을 때 (플레이어 손에 잡혔을 때)
//             _DartRigidbody.isKinematic = true;
//             _DartRigidbody.useGravity = false;
//             _IsFlying = false;
//             // 자연스러운 위치 설정 필요시 offset 조정 추가
//         }
//         else
//         {
//             // 부모가 해제되었을 때 (놓았거나 던져질 때)
//             _DartRigidbody.isKinematic = false;
//             _DartRigidbody.useGravity = true;
//             _IsFlying = true;
//         }
//     }

//     // Fire-and-Forget
    
//     // client(owner)가 호출
//     [ServerRpc(RequireOwnership = false)]
//     public void ThrowDart_ServerRpc(Vector3 velocity, Vector3 angularVelocity)
//     {
//         // if (!IsServer) return;

//         // 부모 해제
//         GetComponent<NetworkObject>().TryRemoveParent();

//         // 서버 물리 연산 시작
//         ApplyPhysics(transform.position, transform.rotation, velocity, angularVelocity);

//         // 모든 클라이언트에 물리 적용 명령 전송
//         ThrowDart_ClientRpc(transform.position, transform.rotation, velocity, angularVelocity);

//     }

//     [ClientRpc]
//     private void ThrowDart_ClientRpc(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
//     {
//         // 서버에서 이미 적용했으므로 서버는 무시
//         if (IsServer) return;

//         _IsVisuallyAttached = false;
//         _VisualTargetBone = null;

//         ApplyPhysics(position, rotation, velocity, angularVelocity);
//     }

//     private void ApplyPhysics(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
//     {
//         transform.position = position;
//         transform.rotation = rotation;

//         _DartRigidbody.isKinematic = false;
//         _DartRigidbody.useGravity = true;
//         _DartRigidbody.linearVelocity = velocity;
//         _DartRigidbody.angularVelocity = angularVelocity;

//         _IsFlying = true;

//         // 자동 파괴 코루틴 시작
//         _MaxLifetimeCoroutine = StartCoroutine(ReturnToPoolAfterMaxLifetime());
//     }

//     // 충돌

//     private void OnCollisionEnter(Collision collision)
//     {
//         if (!IsServer || !_IsFlying) return;

//         // 충돌 위치 보정
//         SyncImpactClientRpc(transform.position, transform.rotation);

//         // "Environment" 태그를 가진 오브젝트와 충돌했을 때
//         if (collision.gameObject.CompareTag("Environment"))
//         {
//             _IsFlying = false;
//             _DartRigidbody.isKinematic = true;
//             _DartRigidbody.useGravity = false;

//             if (_MaxLifetimeCoroutine != null)
//             {
//                 StopCoroutine(_MaxLifetimeCoroutine);
//                 _MaxLifetimeCoroutine = null;
//             }

//             // 충돌 일정 시간 후 반납 코루틴
//             _ReturnCoroutine = StartCoroutine(ReturnToPoolAfterDelay(2.0f));
//         }
//     }

//     [ClientRpc]
//     private void SyncImpactClientRpc(Vector3 position, Quaternion rotation)
//     {
//         // 서버에서 이미 적용했으므로 서버는 무시
//         if (IsServer) return;

//         transform.position = position;
//         transform.rotation = rotation;

//         _DartRigidbody.isKinematic = true;
//         _IsFlying = false;
//     }

//     [ClientRpc]
//     public void AttachToHandClientRpc(bool isRightHand, int socketIndex, ulong ownerClientId)
//     {
//         if (IsServer) return;
//         // 1. 소유자 클라이언트에서는 XRI 소켓에 장착
//         if (NetworkManager.Singleton.LocalClientId == ownerClientId)
//         {
//             AttachToLocalSocket(isRightHand, socketIndex);
//         }
//         else // 2. 타인의 클라이언트에서는 부모 변경 처리
//         {
//             StartAttachToVisualHand(isRightHand);
//         }
//     }

//     private void AttachToLocalSocket(bool isRightHand, int socketIndex)
//     {
//         var activeReloadManager = FindFirstObjectByType<NetworkDartReloadManager>();
//         if (activeReloadManager != null)
//         {
//             activeReloadManager.EquipDartToAssignedSocket(GetComponent<IXRSelectInteractable>(), socketIndex);
//         }
//         else
//         {
//             // 안전장치: ReloadManager가 없으면 그냥 시각적 손에 붙임
//             StartAttachToVisualHand(isRightHand);
//         }
//     }

//     private void StartAttachToVisualHand(bool isRightHand)
//     {
//         if (transform.parent == null)
//         {
//             Debug.Log("Network Dart is not attached to any parent.");
//             return;
//         }

//         var playerSetup = transform.parent.GetComponent<NetworkVRPlayerDriver>();
//         if (playerSetup != null)
//         {
//             Transform targetHand = isRightHand ? playerSetup.NetRightHandIKTarget : playerSetup.NetLeftHandIKTarget;
//             if (targetHand != null)
//             {
//                 _VisualTargetBone = targetHand;
//                 _IsVisuallyAttached = true;

//                 _DartRigidbody.isKinematic = true;
//                 _DartRigidbody.useGravity = false;
//             }
//         }
//     }

//     private IEnumerator ReturnToPoolAfterDelay(float delay)
//     {
//         yield return new WaitForSeconds(delay);

//         GetComponent<NetworkObject>().Despawn(); 
//     }

//     private IEnumerator ReturnToPoolAfterMaxLifetime()
//     {
//         yield return new WaitForSeconds(MaxLifetime);

//         GetComponent<NetworkObject>().Despawn(); 
//     }

// }

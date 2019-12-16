#define TESTING
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * 캐릭터의 Status를 게임중 인스펙터상에서 조절하지 못하도록 차단
 * 캐릭터의 Status를 타 오브젝트에서 변경하지 못하도록 차단.
 */

public class PlayerController : MonoBehaviour
{
    [SerializeField] UIManager UIManager;

    // 상태
    // IDLE         발도 
    // ATTACK       공격중 
    // ROLL         구르기
    // NOSWORDIDLE  납도
    // ETC          기타
    public enum State
    {
        IDLE, ATTACK, ROLL, NOSWORDIDLE, ETC
    }
    public State CState { get; private set; }

    // 데미지를 입은 후 자연치유이냐, 약물을 먹은 후 약물치료이냐를 나누는 열거형
    public enum RecoverCaseType {
        None = 0, Natural = 1, Medicine = 2
    }
    public RecoverCaseType RecoverCase { get; private set; }

    // 코루틴 변수
    private IEnumerator cor;

    private IEnumerator SteminaManager;
    private IEnumerator HPManager;

    // 애니메이터
    private Animator anim;
    private AnimatorStateInfo anistate;

    // 스크립트
    private SwordScript swdscript;

    // GameObject
    public GameObject HealthKit;
    public AudioClip walkSound;

    // 변수
    private float h, v;
    private float DistanceOfRolling = 30f;
    private int chargingcount = 0;
    private bool charging = false;
    private bool running = false;
    private bool nostemina = false;

    private float steminaConsumption;

    // 인스펙터 상에서 값을 조정한 뒤 게임에 적용할 수 있도록 변경.
    // 게임을 시작하면 이후부터는 조정 불가능.

    [Tooltip("캐릭터의 시작HP")] [ReadOnly(true)] [SerializeField] float m_HP;
    [Tooltip("캐릭터의 최대HP")] [ReadOnly(true)] [SerializeField] float m_maxHP;
    [Tooltip("캐릭터의 시작Stemina")] [ReadOnly(true)] [SerializeField] float m_stemina;
    [Tooltip("캐릭터의 최대Stemina")] [ReadOnly(true)] [SerializeField] float m_maxstemina;
    [Tooltip("캐릭터의 회복시간")] [ReadOnly(true)] [SerializeField] float m_recoverTime;
    [Tooltip("캐릭터의 기본 Stemina 회복량")] [ReadOnly(true)] [SerializeField] float m_steminaRecoverValue;

    [Tooltip("달리기 스테미나 소모량")] [SerializeField] float runConsumption;
    [Tooltip("구르기 스테미나 소모량")] [SerializeField] float rollingConsumption;
    [Tooltip("공격 스테미나 소모량")] [SerializeField] float attackConsumption;

    // 타 오브젝트에서 접근할 수 없도록 접근한정자 지정.
    public float HP { get; private set; }
    public float MaxHP { get; private set; }
    public float NaturalRecoverValue { get; private set; }
    public float MedicineRecoverValue { get; private set; }
    public float Stemina { get; private set; }
    public float Maxstemina { get; private set; }
    public float RecoverTime { get; private set; }
    public float WoundVal { get; private set; }
    public float steminaRecoverValue { get; private set; }

    // 1회 회복량을 의미함. 해당 변수값으로 계속 회복하며 정해진 회복량까지 회복을 진행한다.
    private float sequentialNaturalRecoverValue;
    private float sequentialMedicineRecoverValue;
    
    public bool test;
    private bool spendStemina;

    // TransForm Sword
    // TFSWORD_SWORD        발도 상태일때 칼의 위치
    // TFSWORD_NOSWORD      납도 상태일때 칼의 위치
    // TFSWORD_TAKESWORD    칼을 꺼내거나 수납할때 위치
    // TFSWORD              칼의 트랜스폼
    public Transform TFSWORD_SWORD, TFSWORD_NOSWORD, TFSWORD_TAKESWORD;
    public Transform TFSWORD;

    // Audio
    public AudioSource audioSource;

    // 인스펙터에서  입력한 값을 기반으로 캐릭터의 Status를 설정
    void InitVariable()
    {
        HP = m_HP;
        MaxHP = m_maxHP;
        Stemina = m_stemina;
        Maxstemina = m_maxstemina;
        RecoverTime = m_recoverTime;
        steminaRecoverValue = m_steminaRecoverValue;

        SteminaManager = SteminaManage();
        HPManager = HPManage();
    }

    // Use this for initialization
    void Start()
    {
        InitVariable();
        swdscript = GetComponentInChildren(typeof(SwordScript)) as SwordScript;
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();
        anistate = anim.GetCurrentAnimatorStateInfo(0);
        CState = State.NOSWORDIDLE;
        
        StartCoroutine(SteminaManager);
        StartCoroutine(HPManager);
        
    }

    private void FixedUpdate()
    {
        // 데미지입는 것과 회복, 아이템회복관련 테스트를 하기 위했던 구문
#region test
        if (test == true)
        {
            Damege(100);
            test = false;
        }
#endregion

        // 현재 애니메이텨의 상태를 입력받고, 키보드값에 따라 파라미터를 지정한다.
        anistate = anim.GetCurrentAnimatorStateInfo(0);
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        anim.SetFloat("Horizontal", h);
        anim.SetFloat("Vertical", v);

        Quaternion rotation;
        rotation = Quaternion.Euler(transform.rotation.x, h * 150 * Time.deltaTime, transform.rotation.z);

        // 회전
        if (!nostemina)
        {
            if ((h > 0 || h < 0) && CState == State.IDLE || CState == State.NOSWORDIDLE)
                transform.rotation *= rotation;
        }

        // 중력
        GetComponent<CharacterController>().Move(-transform.up * 100 * Time.deltaTime);
        

        // 발도 상태
        if (CState == State.IDLE)
        {
            spendStemina = false;
            // 걷기
            if (false == nostemina)
            {
                if (v > 0)
                {
                    GetComponent<CharacterController>().Move(transform.forward * 10 * Time.deltaTime);
                }
            }

            // 베어 올리기
            if (Input.GetMouseButtonDown(0) && Input.GetMouseButtonDown(1) && CState != State.ATTACK)
            {
                steminaConsumption += attackConsumption;
                StartAnim(RisingSlash());
            }

            // 모아베기
            if (Input.GetMouseButtonDown(0) && CState != State.ATTACK)
            {
                steminaConsumption += attackConsumption;
                StartAnim(ChargedSlash());
            }

            // 모으기 변수
            if (Input.GetMouseButtonDown(0))
            {
                charging = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                charging = false;
            }

            // 베어 넘기기
            if (Input.GetMouseButtonDown(1) && CState != State.ATTACK)
            {
                steminaConsumption += attackConsumption;
                StartAnim(WideSlash());
            }

            // 납도
            if(Input.GetKeyDown(KeyCode.Space))
            {
                anim.SetBool("SWORD", false);
            }
        }

        // 납도 상태
        if (CState == State.NOSWORDIDLE)
        {
            // 이동
            if(!nostemina)
            {
                // 걷기
                if (v > 0)
                {
                    spendStemina = false;
                    GetComponent<CharacterController>().Move(transform.forward * 10 * Time.deltaTime);
                }
                // 달리기
                if (v > 0 && running)
                {
                    spendStemina = true;
                    steminaConsumption += runConsumption;
                    GetComponent<CharacterController>().Move(transform.forward * 30 * Time.deltaTime);
                }
            }

            // 발도
            if (Input.GetMouseButtonDown(0))
            {
                anim.SetBool("SWORD", true);
            }

            // 달리기
            if (Input.GetKey(KeyCode.Space))
            {
                anim.SetBool("Running", true);
                running = true;
            }
            else
            {
                anim.SetBool("Running", false);
                running = false;
            }

            // 구르기
            if(Input.GetKeyDown(KeyCode.LeftShift) && CState != State.ROLL)
            {
                StartAnim(NOSROLLING());
            }
            
            // 아이템
            if(Input.GetKeyDown(KeyCode.E) && CState != State.ETC && (UIManager.CurItemNode.Value.itemAmount > 0))
            {
                StartAnim(USEITEM());
            }
            
        }

        CheckRolling();
    }

    // 회복케이스를 결정하는함수
    public void SetRecoverCase(RecoverCaseType recoverCase)
    {
        this.RecoverCase = recoverCase;
    }

    // 치유될 양을 결정하는 함수
    public void AddRecoverValue(float recoverVal)
    {
        switch(RecoverCase)
        {
            case RecoverCaseType.Medicine:
                MedicineRecoverValue += recoverVal;
                sequentialMedicineRecoverValue = (MedicineRecoverValue / RecoverTime) * Time.fixedDeltaTime;
                break;
            case RecoverCaseType.Natural:
                NaturalRecoverValue = recoverVal;
                sequentialNaturalRecoverValue = (NaturalRecoverValue / RecoverTime) * Time.fixedDeltaTime;
                break;

        }
    }

    // 캐릭터의 체력을 회복하는 함수
    public void Recover()
    {
        if (sequentialMedicineRecoverValue > 0f)
        {
            HP += sequentialMedicineRecoverValue;
            if ((MedicineRecoverValue - sequentialMedicineRecoverValue) <= 0f)
            {
                MedicineRecoverValue = 0f;
                sequentialMedicineRecoverValue = 0f;
            }
            else
            {
                MedicineRecoverValue -= sequentialMedicineRecoverValue;
            }
        }
        if (sequentialNaturalRecoverValue > 0f)
        {
            HP += sequentialNaturalRecoverValue;
            WoundVal = ((WoundVal - sequentialNaturalRecoverValue) <= 0f) ? 0f : WoundVal - sequentialNaturalRecoverValue;
            if((NaturalRecoverValue - sequentialNaturalRecoverValue) <= 0f)
            {
                NaturalRecoverValue = 0f;
                sequentialNaturalRecoverValue = 0f;
            }
            else
            {
                NaturalRecoverValue -= sequentialNaturalRecoverValue;
            }
        }

        if (HP >= MaxHP)
        {
            HP = MaxHP;
            WoundVal = 0f;
            RecoverCase = RecoverCaseType.None;
            MedicineRecoverValue = 0f; sequentialMedicineRecoverValue = 0f;
            NaturalRecoverValue = 0f; sequentialNaturalRecoverValue = 0f;
        }

        //Debug.Log("Recover is Done. HP : " + HP);
    }


    // 타 오브젝트에서 캐릭터 상태를 변경할 경우 사용하기 위한 함수
    public void SetState(State state)
    {
        CState = state;
    }

    // 꺼내거나 수납 중일때 위치 변경
    public void TAKINGSWORD()
    {
        SoundManager.Instance.PlaySound(audioSource, "Weapon", "EquipSword");
        TFSWORD.parent = TFSWORD_TAKESWORD;
        TFSWORD.rotation = new Quaternion(0, 0, 0, 0);
        TFSWORD.localPosition = Vector3.zero;
    }

    // 발도 상태 일때 위치 변경
    public void TAKESWORD()
    {
        TFSWORD.parent = TFSWORD_SWORD;
        TFSWORD.rotation = new Quaternion(0, 0, 0, 0);
        TFSWORD.localPosition = Vector3.zero;
    }

    // 납도 상태 일때 위치 변경
    public void TAKEOUTSWORD()
    {
        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Equip");
        TFSWORD.parent = TFSWORD_NOSWORD;
        TFSWORD.rotation = new Quaternion(0, 0, 0, 0);
        TFSWORD.localPosition = Vector3.zero;
    }

    // 애니메이션 재생
    void StartAnim(IEnumerator action)
    {
        // 현재 실행중인 코루틴 중지
        if (cor != null)
        {
            Debug.Log("StopCoroutine's state : " + CState);
            StopCoroutine(cor);
        }

        // 현재 코루틴 저장 후 코루틴 실행
        cor = action;
        StartCoroutine(cor);
        Debug.Log("State : " + CState.ToString());
    }

    // 구르기
    void CheckRolling()
    {
        if(v < 0 && Input.GetKeyDown(KeyCode.LeftShift) && CState == State.IDLE)
        {
            StartAnim(BackRolling());
            return;
        }

        else if (Input.GetKeyDown(KeyCode.LeftShift) && CState == State.IDLE)
        {
            StartAnim(Rolling());
            return;
        }
    }

    // 현재 재생중인 애니메이션의 진행률을 0~1 까지의 실수로 받아옴
    float WaitForAnimation()
    {   
            return anim.GetCurrentAnimatorStateInfo(0).normalizedTime + float.Epsilon + Time.deltaTime;
    }
    
    // 피격
    public void Damege(int _dmg)
    {
        if (CState != State.ROLL)
        {
            // 납도 상태일떄
            if (CState == State.NOSWORDIDLE)
                StartAnim(ATTACKED());

            // 공격중이거나 발도 상태일때 
            else if (CState == State.ATTACK || CState == State.IDLE)
                StartAnim(SWORDATTACKED());

            // 입은 데미지의 일부를 부상값으로 지정하고, 회복케이스를 자연치유로 변경
            WoundVal = (_dmg * 0.6f);
            RecoverCase = RecoverCaseType.Natural;

            // 이전에 사용한 아이템이 없거나, 포션류가 아닌 경우
            if ((UIManager.UsedItem ==null ) || UIManager.UsedItem.itemType != ItemClass.ItemType.Potion)
            {
                RecoverCase = RecoverCaseType.Natural;
                AddRecoverValue(WoundVal);
            }
            // 이전에 사용한 아이템이 연통류 일 경우
            else if(UIManager.UsedItem.itemType == ItemClass.ItemType.RecoveryGas)
            {
                RecoverCase = RecoverCaseType.Natural;
                AddRecoverValue(WoundVal);
            }
            // 만일 이전에 사용한 아이템이 포션류일경우,
            else if (UIManager.UsedItem.itemType == ItemClass.ItemType.Potion)
            {
                RecoverCase = RecoverCaseType.Natural;
                AddRecoverValue(WoundVal);
            }

            HP -= _dmg;
            Debug.Log("현재 플레이어 HP: " + HP);
        }
    }

    public void WalkSoundPlay()
    {
        audioSource.volume = 0.2f;
        audioSource.PlayOneShot(walkSound);
        audioSource.volume = 1f;
    }
    
    IEnumerator SteminaManage()
    {
        WaitForSeconds deltatime = new WaitForSeconds(Time.fixedDeltaTime);
        WaitForSeconds NoSteminaAnimationTime = new WaitForSeconds(6f);
        while(true)
        {
             //스테미너 소비를 진행.
            Stemina = ((Stemina - steminaConsumption) <= 0f) ? 0f : Stemina - steminaConsumption;
            steminaConsumption = 0f;

            anim.SetFloat("Stemina", Stemina);
            // 현재의 스테미너가 0 미만으로 떨어진 경우, 
            // 0으로 초기화. 스테미너 없음을 변수화, 스테미너 소비를 정지, 해당 애니메이션 시간만큼 대기
            if (Stemina == 0f)
            {
                anim.Play("NOSTEMINA");
                nostemina = true;
                spendStemina = false;
                CState = State.ETC;
                anim.SetBool("Running", false);
                yield return NoSteminaAnimationTime;
                nostemina = false;
                CState = State.NOSWORDIDLE;
            }

            // 만일 스테미너가 없지 않다면
            if (false == nostemina && false == spendStemina)
            {
                // 캐릭터의 상태별로 스테미너 회복량을 다르게 설정
                switch (CState)
                {
                    case State.NOSWORDIDLE:
                        // 무기없는 대기상태일때 보다 많은 스테미나를 회복해야함.
                        Stemina = ((Stemina + (steminaRecoverValue * 1.5f)) >= Maxstemina) ? Maxstemina : Stemina + (steminaRecoverValue * 1.5f);
                        break;
                    case State.IDLE:
                        // 발도 대기 일때는 보다 적은 스테미나를 회복해야함.
                        Stemina = ((Stemina + steminaRecoverValue) >= Maxstemina) ? Maxstemina : Stemina + steminaRecoverValue;
                        break;
                    default:
                        break;
                }
            }
            yield return deltatime;
        }
    }

    IEnumerator HPManage()
    {
        WaitForSeconds deltatime = new WaitForSeconds(Time.fixedDeltaTime);
        while(true)
        {
            if(HP <= 0)
            {
                StartAnim(DEAD());
            }
            if(sequentialMedicineRecoverValue > 0f || sequentialNaturalRecoverValue > 0f)
            {
                Recover();
            }
            yield return deltatime;
        }
    }
    //IEnumerator DEAD()
    //{
    //    cState = State.ETC;                           상태 지정
    //    anim.Play("DEAD");                            애니메이션 재생
    //    Debug.Log("플레이어 죽음...");

    //    while (true)                                  코루틴
    //    {
    //        if (anistate.IsName("DEAD"))              현재 애니메이션 이름 검사
    //        {
    //            if (WaitForAnimation() > 1f)          애니메이션 진행 중 실행할 코드
    //            {                                     ex) WaitForAnimation() > 1f : 애니메이션이 끝나면 실행
    //                break;                            ex) WaitForAnimation() > 0f && WaitForAnimation() < 0.5f
    //            }                                         애니메이션 진행 0% 부터 50% 사이에 작성한 코드 계속 실행
    //        }

    //        yield return new WaitForFixedUpdate();
    //    }

    //     cState = State.NOSWORDIDLE;                  상태 지정
    //}

    // NOSWORD IDLE Animation script    ====================================================================

    // 납도 상태 일때 피격
    IEnumerator ATTACKED()
    {
        CState = State.ETC;
        chargingcount = 0;
        SoundManager.Instance.PlaySound(audioSource, "Player", "Attacked");
        anim.Play("ATTACKED-1");

        while (true)
        {
            if (anistate.IsName("ATTACKED-1"))
            {
                GetComponent<CharacterController>().Move(-transform.forward * 50f * Time.deltaTime);
            }


            if (anistate.IsName("ATTACKED-2"))
            {
                
            }

            if (anistate.IsName("ATTACKED-3"))
            {
                
            }

            if (anistate.IsName("ATTACKED-4"))
            {
                if (WaitForAnimation() > 1f)
                {
                    Debug.Log("Player Wake Up");
                    break;
                }
            }

            yield return null;
        }

        CState = State.NOSWORDIDLE;
    }

    IEnumerator DEAD()
    {
        CState = State.ETC;
        chargingcount = 0;
        StopCoroutine(HPManager);
        StopCoroutine(SteminaManager);
        SoundManager.Instance.PlaySound(audioSource, "Player", "Death");
        Stemina = 0f;
        anim.Play("DEAD");
        Debug.Log("플레이어 죽음...");

        while (true)
        {
            if (anistate.IsName("DEAD"))
            {
                if (WaitForAnimation() > 1f)
                {
                    break;
                }
            }
            yield return null;
        }
    }
    
    /*
     * 코루틴 작동시 캐릭터의 cState 값을 변경, 애니메이션을 작동시키고, 
     * 내부 루프를 진행한다. 
     */
    // 아이템 사용을 위한 코루틴
    IEnumerator USEITEM()
    {
        ItemClass.Item curItem;
        CState = State.ETC;
        chargingcount = 0;
        spendStemina = false;
        anim.Play("ITEM");
        anistate = anim.GetCurrentAnimatorStateInfo(0);

        bool flag = true;
        while (true)
        {
            if (anistate.IsName("ITEM") && (WaitForAnimation() >= 0.5f) && flag)
            {
                curItem = UIManager.UseItem();
                HealthKit.GetComponent<HealthKitScript>().itemVal = curItem;

                Instantiate(HealthKit, transform.position, Quaternion.identity);

                flag = false;
                yield return new WaitWhile(() => anistate.IsName("ITEM"));
                break;
            }
            yield return null;
        }
        CState = State.NOSWORDIDLE;
    }

    IEnumerator NOSROLLING()
    {
        chargingcount = 0;
        CState = State.ROLL;
        SoundManager.Instance.PlaySound(audioSource, "Player", "Roll");
        anim.CrossFade("NOSROLLING", 0.1f);
        steminaConsumption += rollingConsumption;

        while (true)
        {
            if (anistate.IsName("NOSROLLING"))
            {
                if (WaitForAnimation() < 0.5f)
                {
                    transform.Translate(Vector3.forward * DistanceOfRolling * Time.deltaTime);
                }

                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }

        CState = State.NOSWORDIDLE;
    }

    // SWORD IDLE Animation script      ====================================================================

    // 발도 상태일때 피격
    IEnumerator SWORDATTACKED()
    {
        CState = State.ETC;
        chargingcount = 0;
        anim.Play("SWORDATTACKED-1");
        SoundManager.Instance.PlaySound(audioSource, "Player", "Attacked");
        while (true)
        {
            if (anistate.IsName("SWORDATTACKED-1"))
            {
                GetComponent<CharacterController>().Move(-transform.forward * 50f * Time.deltaTime);
            }


            if (anistate.IsName("SWORDATTACKED-2"))
            {

            }

            if (anistate.IsName("SWORDATTACKED-3"))
            {

            }

            if (anistate.IsName("SWORDATTACKED-4"))
            {
                if (WaitForAnimation() > 1f)
                {
                    break;
                }
            }

            yield return null;
        }

        CState = State.IDLE;
    }

    // 태클
    IEnumerator Tackle()
    {
        CState = State.ROLL;
        anim.CrossFade("Tackle", 0.3f);
        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Tackle");
        steminaConsumption += attackConsumption;

        while (true)
        {
            if (anistate.IsName("Tackle"))
            {

                if (WaitForAnimation() > 0.3f && WaitForAnimation() < 1f)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        chargingcount++;
                        StartAnim(TackleCharged());
                    }

                    if (Input.GetMouseButtonDown(1))
                        StartAnim(JumpingWideSlash());
                }
                else if(WaitForAnimation() > 1f)
                    break;
                    
            }

            yield return null;
        }

        CState = State.IDLE;
    }

    // 태클 후 모아베기 모으는 애니메이션
    IEnumerator TackleCharged()
    {
        CState = State.ATTACK;
        bool soundflag = true;

        if (chargingcount == 1)
        {
            anim.CrossFade("TackleStrongCharging", 0.3f);

            while (true)
            {
                if (anistate.IsName("TackleStrongCharging"))
                {
                    if (WaitForAnimation() > 0.1f && soundflag)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing3");
                        soundflag = false;
                    }

                    if (WaitForAnimation() > 1f)
                        StartAnim(Charging());
                }

                yield return null;
            }
        }
        else
        {
            anim.CrossFade("TackleTrueCharging", 0.3f);

            while (true)
            {
                if (anistate.IsName("TackleTrueCharging"))
                {
                    if (WaitForAnimation() > 0.1f && soundflag)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing3");
                        soundflag = false;
                    }

                    if (WaitForAnimation() > 1f)
                        StartAnim(Charging());
                }

                yield return null;
            }
        }


    }

    // 모아베기 준비자세
    IEnumerator ChargedSlash()
    {
        CState = State.ATTACK;
        bool soundflag = true;

        if (chargingcount == 0)
        {
            anim.CrossFade("ChargedSlash1-1",0.1f);

            while (true)
            {
                if (anistate.IsName("ChargedSlash1-1"))
                {
                    if (WaitForAnimation() > 0.1f && soundflag)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing3");
                        soundflag = false;
                    }

                    if (WaitForAnimation() > 0.78f && WaitForAnimation() < 1f)
                    {
                        if (charging)
                            StartAnim(Charging());
                    }
                    else if (WaitForAnimation() > 1f)
                        StartAnim(Charging());
                }

                yield return null;
            }
        }
        else if (chargingcount == 1)
        {
            anim.CrossFade("StrongChargedSlash-1", 0.1f);

            while (true)
            {
                if (anistate.IsName("StrongChargedSlash-1"))
                {
                    if (WaitForAnimation() > 0.1f && soundflag)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing2");
                        soundflag = false;
                    }

                    if (WaitForAnimation() > 0.95f && WaitForAnimation() < 1f)
                    {
                        if (charging)
                            StartAnim(Charging());
                    }
                    else if (WaitForAnimation() > 1f)
                        StartAnim(Charging());
                }

                yield return null;
            }
        }
        else
        {
            anim.CrossFade("TrueChargedSlash-1", 0.1f);
            bool soundflag2 = true;

            while (true)
            {
                if (anistate.IsName("TrueChargedSlash-1"))
                {
                    if (WaitForAnimation() > 0.1f && soundflag)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing2");
                        soundflag = false;
                    }

                    if (WaitForAnimation() > 0.7f && soundflag2)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing3");
                        soundflag2 = false;
                    }

                    if (WaitForAnimation() > 0.95f && WaitForAnimation() < 1f)
                    {
                        if (charging)
                            StartAnim(Charging());
                    }
                    else if (WaitForAnimation() > 1f)
                        StartAnim(Charging());
                }

                yield return null;
            }
        }
        
        
    }

    // 모아베기 공격
    IEnumerator ChargedSlashAttack()
    {
        
        bool atkflag = true;
        CState = State.ATTACK;

        if (chargingcount == 0)
        {
            anim.CrossFade("ChargedSlash1-3", 0.3f);
            bool soundflag = true;

            while (true)
            {
                if (anistate.IsName("ChargedSlash1-3"))
                {
                    if (WaitForAnimation() > 0.1f && soundflag)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing4");
                        soundflag = false;
                    }

                    if (WaitForAnimation() > 0.2f && !swdscript.attacked && atkflag)
                    {
                        atkflag = false;
                        swdscript.attacked = true;
                    }

                    if (WaitForAnimation() < 0.3f)
                    {
                        transform.Translate(Vector3.forward * 10 * Time.deltaTime);

                    }
                    else if (WaitForAnimation() > 0.3f && WaitForAnimation() < 0.8f)
                    {
                        if (h < 0 && Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(LeftRolling());
                        else if (h > 0 && Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(RightRolling());
                        else if (Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(Rolling());

                        if (Input.GetMouseButtonDown(0) && v > 0)
                        {
                            chargingcount++;
                            StartAnim(ChargedSlash());
                        }
                        else if (Input.GetMouseButtonDown(0))
                            StartAnim(SideBlow());

                    }
                    else
                        break;
                }

                yield return null;
            }
        }
        else if (chargingcount == 1)
        {
            anim.CrossFade("StrongChargedSlash-2", 0.3f);
            bool soundflag = true;
            while (true)
            {
                if (anistate.IsName("StrongChargedSlash-2"))
                {
                    if(WaitForAnimation() > 0.1f && soundflag)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing5");
                        soundflag = false;
                    }

                    if (WaitForAnimation() > 0.2f && !swdscript.attacked && atkflag)
                    {
                        atkflag = false;
                        swdscript.attacked = true;
                    }

                    if (WaitForAnimation() < 0.3f)
                    {
                        transform.Translate(Vector3.forward * 10 * Time.deltaTime);

                    }
                    else if (WaitForAnimation() > 0.3f && WaitForAnimation() < 0.8f)
                    {
                        if (h < 0 && Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(LeftRolling());
                        else if (h > 0 && Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(RightRolling());
                        else if (Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(Rolling());

                        if (Input.GetMouseButtonDown(0) && v > 0)
                        {
                            chargingcount++;
                            StartAnim(ChargedSlash());
                        }
                        else if (Input.GetMouseButtonDown(0))
                            StartAnim(SideBlow());

                    }
                    else
                        break;
                }

                yield return null;
            }
        }
        else
        {
            anim.CrossFade("TrueChargedSlash-2", 0.3f);
            bool trueAtkflag = false;
            bool soundflag1 = true;
            bool soundflag2 = true;
            while (true)
            {
                if (anistate.IsName("TrueChargedSlash-2"))
                {
                    if(WaitForAnimation() > 0.0f && soundflag1)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing4");
                        soundflag1 = false;
                    }

                    if (WaitForAnimation() > 0.25f && soundflag2)
                    {
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Swing1");
                        soundflag2 = false;
                    }

                    if (WaitForAnimation() > 0.1f&& !swdscript.attacked && atkflag)
                    {
                        atkflag = false;
                        swdscript.attacked = true;
                        trueAtkflag = true;
                    }

                    
                    if (WaitForAnimation() > 0.3f && !swdscript.attacked && trueAtkflag)
                    {
                        trueAtkflag = false;
                        swdscript.attacked = true;
                    }

                    if (WaitForAnimation() < 0.3f)
                    {
                        transform.Translate(Vector3.forward * 10 * Time.deltaTime);

                    }
                    else if (WaitForAnimation() > 0.3f && WaitForAnimation() < 0.8f)
                    {
                        if (h < 0 && Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(LeftRolling());
                        else if (h > 0 && Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(RightRolling());
                        else if (Input.GetKeyDown(KeyCode.LeftShift))
                            StartAnim(Rolling());

                        if (Input.GetMouseButtonDown(0))
                            StartAnim(SideBlow());

                    }
                    else
                        break;
                }

                yield return null;
            }
        }

        chargingcount = 0;
        CState = State.IDLE;
    }

    // 모아베기 모으기
    IEnumerator Charging()
    {
        CState = State.ATTACK;
        
        bool flag = false;

        if (chargingcount == 0)
        {
            anim.CrossFade("Charging", 0.1f);

            while (true)
            {
                if (anistate.IsName("Charging"))
                {
                    if (charging == false)
                        StartAnim(ChargedSlashAttack());

                    if(Input.GetMouseButtonUp(0))
                        StartAnim(ChargedSlashAttack());

                    if (Input.GetMouseButtonDown(1))
                        StartAnim(Tackle());

                    if (WaitForAnimation() > 0.4f && !flag)
                    {
                        flag = true;
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Charge");
                        GetComponent<PlayerOutlineController>().ActiveOutLineStrong();
                    }

                    if (WaitForAnimation() > 1f)
                    {
                        StartAnim(ChargedSlashAttack());
                    }

                }

                yield return null;
            }
        }
        else if (chargingcount == 1)
        {
            anim.CrossFade("StrongCharging", 0.1f);

            while (true)
            {
                if (anistate.IsName("StrongCharging"))
                {
                    if (Input.GetMouseButtonDown(1))
                        StartAnim(Tackle());
                    if (charging == false)
                        StartAnim(ChargedSlashAttack());
                    if (Input.GetMouseButtonUp(0))
                        StartAnim(ChargedSlashAttack());

                    if (WaitForAnimation() > 0.4f && !flag)
                    {
                        flag = true;
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Charge");
                        GetComponent<PlayerOutlineController>().ActiveOutLineStrong();
                    }

                    if (WaitForAnimation() > 1f)
                    {
                        StartAnim(ChargedSlashAttack());
                    }
                }

                yield return null;
            }
        }
        else
        {
            anim.CrossFade("TrueCharging", 0.1f);

            while(true)
            {
                if (anistate.IsName("TrueCharging"))
                {
                    if (Input.GetMouseButtonDown(1))
                        StartAnim(Tackle());
                    if (charging == false)
                        StartAnim(ChargedSlashAttack());
                    if (Input.GetMouseButtonUp(0))
                        StartAnim(ChargedSlashAttack());

                    if (WaitForAnimation() > 0.4f && !flag)
                    {
                        flag = true;
                        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Charge");
                        GetComponent<PlayerOutlineController>().ActiveOutLineStrong();
                    }

                    if (WaitForAnimation() > 1f)
                    {
                        StartAnim(ChargedSlashAttack());
                    }
                }

                yield return null;
            }
        }
       
    }

    // 옆으로 치기
    IEnumerator SideBlow()
    {
        CState = State.ATTACK;
        chargingcount = 0;
        anim.CrossFade("SideBlow", 0.3f);
        steminaConsumption += attackConsumption;
        bool atkflag = true;

        while(true)
        {
            if(anistate.IsName("SideBlow"))
            {
                if (WaitForAnimation() > 0.1f && !swdscript.attacked && atkflag)
                {
        SoundManager.Instance.PlaySound(audioSource, "Weapon", "Tackle");
                    atkflag = false;
                    swdscript.attacked = true;
                }

                if (WaitForAnimation() > 0.6f)
                {
                    atkflag = false;
                    swdscript.attacked = false;
                }

                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }

        CState = State.IDLE;
    }

    // 베어 넘기기
    IEnumerator WideSlash()
    {
        CState = State.ATTACK;
        anim.CrossFade("WideSlash", 0.3f);
        steminaConsumption += attackConsumption;
        bool atkflag = true;

        while (true)
        {
            if (anistate.IsName("WideSlash"))
            {
                if (WaitForAnimation() > 0.2f && !swdscript.attacked && atkflag)
                {
                    atkflag = false;
                    swdscript.attacked = true;
                }

                if (WaitForAnimation() > 0.5f && WaitForAnimation() < 1f)
                {
                    if (Input.GetMouseButtonDown(0) && Input.GetMouseButtonDown(1))
                        StartAnim(RisingSlash());

                    else if (Input.GetMouseButtonDown(0))
                        StartAnim(ChargedSlash());

                    else if (Input.GetMouseButtonDown(1))
                        StartAnim(Tackle());
                }

                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }

        CState = State.IDLE;
    }

    // 베어 올리기
    IEnumerator RisingSlash()
    {
        CState = State.ATTACK;
        anim.CrossFade("RisingSlash", 0.3f);
        steminaConsumption += attackConsumption;
        bool atkflag = true;

        while (true)
        {
            if (anistate.IsName("RisingSlash"))
            {
                if (WaitForAnimation() > 0.2f && !swdscript.attacked && atkflag)
                {
                    atkflag = false;
                    swdscript.attacked = true;
                }

                if (WaitForAnimation() > 0.5f && WaitForAnimation() < 1f)
                {
                    if (Input.GetMouseButtonDown(0))
                        StartAnim(ChargedSlash());

                    else if (Input.GetMouseButtonDown(1))
                        StartAnim(WideSlash());
                }

                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }

        CState = State.IDLE;
    }

    // 뛰어들어 베어넘기기
    IEnumerator JumpingWideSlash()
    {
        CState = State.ATTACK;
        anim.CrossFade("JumpingWideSlash", 0.3f);
        steminaConsumption += attackConsumption;
        bool atkflag = true;

        while (true)
        {
            if (WaitForAnimation() > 0.2f && !swdscript.attacked && atkflag)
            {
                atkflag = false;
                swdscript.attacked = true;
            }

            if (anistate.IsName("JumpingWideSlash"))
            {
                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }

        CState = State.IDLE;
    }


    // 구르기
    IEnumerator Rolling()
    {
        chargingcount = 0;
        CState = State.ROLL;
        anim.CrossFade("Rolling", 0.001f);
        SoundManager.Instance.PlaySound(audioSource, "Player", "Roll");
        steminaConsumption += rollingConsumption;



        while (true)
        {
            if (anistate.IsName("Rolling"))
            {
                if (WaitForAnimation() < 0.5f )
                {
                    transform.Translate(Vector3.forward * DistanceOfRolling * Time.deltaTime);
                }

                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }

        CState = State.IDLE;
    }

    IEnumerator LeftRolling()
    {
        chargingcount = 0;
        CState = State.ROLL;
        SoundManager.Instance.PlaySound(audioSource, "Player", "Roll");
        steminaConsumption += rollingConsumption;
        anim.CrossFade("LeftRolling", 0.001f);

        while (true)
        {
            if (anistate.IsName("LeftRolling"))
            {
                if (WaitForAnimation() < 0.5f)
                {
                    transform.Translate(Vector3.left * DistanceOfRolling * Time.deltaTime);
                }

                if (WaitForAnimation() > 1f)
                    break;
            }
            yield return null;
        }

        CState = State.IDLE;
    }

    IEnumerator RightRolling()
    {
        chargingcount = 0;
        CState = State.ROLL;
        SoundManager.Instance.PlaySound(audioSource, "Player", "Roll");
        steminaConsumption += rollingConsumption;
        anim.CrossFade("RightRolling", 0.001f);

        while (true)
        {
            if (anistate.IsName("RightRolling"))
            {
                if (WaitForAnimation() < 0.5f)
                {
                    transform.Translate(Vector3.right * DistanceOfRolling * Time.deltaTime);
                }

                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }

        CState = State.IDLE;
    }

    IEnumerator BackRolling()
    {
        chargingcount = 0;
        CState = State.ROLL;
        SoundManager.Instance.PlaySound(audioSource, "Player", "Roll");
        steminaConsumption += rollingConsumption;
        anim.CrossFade("BackRolling", 0.001f);
        while (true)
        {
            if (anistate.IsName("BackRolling"))
            {
                if (WaitForAnimation() < 0.5f)
                {
                    transform.Translate(Vector3.back * DistanceOfRolling * Time.deltaTime);
                }

                if (WaitForAnimation() > 1f)
                    break;
            }

            yield return null;
        }
        CState = State.IDLE;
    }
}
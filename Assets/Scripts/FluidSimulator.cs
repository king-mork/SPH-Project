using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSimulator : MonoBehaviour
{
    //Constants
    const float GAS = -40.0f;
    const float GRAV = 1.0f;
    const float VISC = 1.0f;
    

    //Simulation Attributes
    private GameObject cup;
    private CupCollider[] colliders;
    private Particle[] particles;
    private int cNum = 5;   //Number of colliders in cup
    private float restDensity;
    private int pNumActive;

    //Togglable attributes
    public int pNum;    //Number of particles
    public Vector3 g;   //Gravity
    public float r;     //Particle radius
    public float m;     //Particle mass
    public float ks;    //Spring coefficient
    public float kd;    //Drag coefficient
    public float smoothRad; //Smoothing Radius
    public bool move;


    void Awake(){
        cup = GameObject.Find("Cup");
        pNumActive = 0;
    }

    // Start is called before the first frame update
    void Start()
    {   
        //Set physics engine to ignore collisions between water particles
        initColliders();
        //initParticles();
        restDensity = m*pNum;
        particles = new Particle[pNum];
    }

    private void initParticles(){   //**Create and initialize particles
        //Get particle prefab
        GameObject particle_prefab;
        //Create particle array
        //particles = new Particle[pNum];

        //for(int i = 0; i < pNum; i++){
            //Instantiate particle object
            particle_prefab = (GameObject)Resources.Load("Water_Particle");
            GameObject go  = Instantiate(particle_prefab);
            particles[pNumActive] = go.AddComponent<Particle>();
            particles[pNumActive].setObject(go);

            //Generate start position
            float x = Random.Range(-0.5f, 0.5f);
            float z = Random.Range(-0.5f, 0.5f);
            Vector3 p0 = new Vector3(x, 1.5f, z);

            //Scale object
            particles[pNumActive].setRadius(r);
            //Translate object to start position
            particles[pNumActive].setPosition(p0);
        //}
        pNumActive++;
    }

    
    private void initColliders(){   //**Find and store all collider objects
        //Initialize collider array
        colliders = new CupCollider[cNum];
        //Find all colliders by tag: "sph_collider"
        GameObject[] colliderList = GameObject.FindGameObjectsWithTag("sph_collider");
        //For every collider found, initialize
        for(int i = 0; i < cNum; i++){
            GameObject go = colliderList[i];
            colliders[i] = new CupCollider();
            colliders[i].setPosition(go.transform.position);
            colliders[i].setRight(go.transform.right);
            colliders[i].setUp(go.transform.up);
            colliders[i].setScale(go.transform.localScale);
        }
    }
    
    private void updateColliders(){
        //Rotate cup
        cup.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 40.0f * Mathf.Sin(Time.time));
        //Find all colliders by tag: "sph_collider"
        GameObject[] colliderList = GameObject.FindGameObjectsWithTag("sph_collider");
        //For every collider found, update
        for(int i = 0; i < cNum; i++){
            GameObject go = colliderList[i];
            colliders[i].setPosition(go.transform.position);
            colliders[i].setRight(go.transform.right);
            colliders[i].setUp(go.transform.up);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        //Rotate Cup
        if(move && Time.time > 3.0f)
            updateColliders();

        //Spawn particles if any remain
        if(pNumActive < pNum){
            initParticles();
        }

        //Calculate density and pressure for each particle
        for(int i = 0; i < pNumActive; i++){
            calcDensityPressure(i);
        }
        //Calculate total force acting on each particle
        for(int i = 0; i < pNumActive; i++){
            Vector3 Fg = Vector3.zero, Fp = Vector3.zero, Fv = Vector3.zero;

            //Calculate force of gravity
            Fg = GRAV * g * particles[i].getDensity();
            //Calculate force of each neighboring particle
            for(int j = 0; j < pNumActive; j++){
                if(i == j)
                    continue;

                //Distance and direction                
                Vector3 dir = particles[j].getPosition() - particles[i].getPosition();
                float dist = dir.magnitude; 

                //Calculate force of pressure
                Fp += forcePressure(i, j, dir, dist);
                //Calculate viscocity force
                Fv += forceViscocity(i, j, dir, dist);
            }
            
            //Calculate total force on particle
            Vector3 Ftotal = Fp + Fg + Fv;
            //Set total force acting on particle
            particles[i].setForce(Ftotal);
        }
        
        //Set new positions
        moveParticles(dt);
    }

    private void calcDensityPressure(int i){     //**Calculates density and pressure of a given particle
        //Calc Density
        float density = 0.0f;
        for(int j = 0; j < pNumActive; j++){
            if(i == j)
                continue;
            Vector3 dir = particles[j].getPosition() - particles[i].getPosition();
            float dist = dir.magnitude;

            //Poly6 kernel
            if(dist < smoothRad){
                density += m * (315.0f / (64.0f * Mathf.PI * Mathf.Pow(smoothRad, 9.0f))) * Mathf.Pow(smoothRad - dist, 3.0f);
            }
        }
        particles[i].setDensity(Mathf.Max(density, restDensity));
        //Calc pressure
        particles[i].setPressure(GAS*(particles[i].getDensity()-restDensity));
    }

    private Vector3 forcePressure(int i, int j, Vector3 dir, float dist){    //**Calculates force of pressure
        Vector3 Fp = Vector3.zero;
        Vector3 dirUnit = dir.normalized;
        
        //Spikey gradient
        if(dist < smoothRad){
            Fp += -1.0f * dirUnit * m * (particles[i].getPressure() + particles[j].getPressure()) / (2.0f * particles[j].getDensity()) 
            * (-45.0f / (Mathf.PI * Mathf.Pow(smoothRad, 6.0f))) * Mathf.Pow(smoothRad - dist, 2.0f);
        }

        return Fp;
    }

    private Vector3 forceViscocity(int i, int j, Vector3 dir, float dist){  //**Calculates force of viscocity
        Vector3 Fv = Vector3.zero;
        
        //Viscosity Laplacian
        if(dist < smoothRad){
            Fv += VISC * m * (particles[i].getVelocity() - particles[j].getVelocity()) 
            / particles[j].getDensity() * (45.0f / (Mathf.PI * Mathf.Pow(smoothRad, 6.0f))) * (smoothRad - dist);
        }

        return Fv;
    }

    private void moveParticles(float dt){   //**Derives the velocity and position of each particle

        for(int i = 0; i < pNumActive; i++){
            Particle p = particles[i];
            //Get initial position and velocity
            Vector3 iVel = p.getVelocity(), iPos = p.getPosition();
            Vector3 vel, pos;
            
            //Interpolate velocity
            //Newton's 2nd Law (acceleration)
            Vector3 a = p.getForce()/p.getDensity();
            //Calc velocity
            vel = iVel + (a * dt);
            
            //Check for collision against every collider
            for(int j = 0; j < cNum; j++){
                Vector3 cNormal;
                if( isColliding(iPos, vel, colliders[j], out cNormal, dt) ){
                    //Resolve collision velocity
                    vel = resolveCollision(colliders[j], vel, cNormal);
                    //Penalty
                    iPos += (0.03f * cNormal);
                }
            }
            
            //Calc position
            pos = iPos + (vel * dt);

            //Set vel and pos in particle
            p.setVelocity(vel);
            p.setPosition(pos);
        }

    }

    private bool isColliding(Vector3 pos, Vector3 vel, CupCollider col, out Vector3 cNormal, float dt){

        Vector3 cPos = col.pos;
        //Get normal
        cNormal = col.up;
        //x-p
        Vector3 tmp1 = pos - cPos;
        //dot((x-p), n)
        double dist = Mathf.Abs(Vector3.Dot(tmp1, cNormal));
        //Get distance of next position
        Vector3 posNext = pos + (vel * dt);
        Vector3 tmp2 = posNext - cPos;
        double distNext = Mathf.Abs(Vector3.Dot(tmp2, cNormal));
        //Check if close to collider and moving toward it
        if(dist - (r/2.0f) < 0.0f && distNext < dist){
            //Debug.Log("COLLISION");
            return true;
        }

        return false;
    }

    private Vector3 resolveCollision(CupCollider col, Vector3 vel, Vector3 cNormal){
        
        Vector3 adj = Vector3.Cross(col.up, col.right);

        Vector3 nVel = (ks * Vector3.Dot(vel, cNormal) * cNormal) 
        + ((1.0f - kd) * Vector3.Dot(vel, col.right) * col.right)
        + ((1.0f - kd) * Vector3.Dot(vel, adj) * adj);

        //Debug.Log("cNormal:" +cNormal+ " iVel: " +vel+ " nVel:" +nVel);

        return nVel;
    }

}

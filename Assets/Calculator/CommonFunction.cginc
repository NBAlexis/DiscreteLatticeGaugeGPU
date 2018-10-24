
//This function return 4-coordinate
//----------------------------------------
//sites are sitenumber x sitenumber x sitenumber x sitenumber x dir (max is 2^26 - 1)
//dir is always forward, so site (0,0,0,0)-d is (0,0,0,0,d)
//there are 2^(D-1) plaquettes
//----------------------------------------
inline int GetGvalueG4096_4D(RWTexture2D<int2> conf, uint2 site, uint dir)
{
    if (0 == dir)
    {
        return conf[site].r >> 12;
    }
    else if (1 == dir)
    {
        return conf[site].r & 4095;
    }
    else if (2 == dir)
    {
        return conf[site].g >> 12;
    }
    return conf[site].g & 4095;
}

//-------------------------------------
//L site index to uint2 site index
//-------------------------------------
inline uint2 GetSiteIndexG4096_4D(uint L, uint isitefhift, uint siteMask)
{
    return uint2((((L >> (3 * (isitefhift + 1))) & siteMask) << isitefhift)
                + ((L >> (2 * (isitefhift + 1))) & siteMask),
                 (((L >> (1 * (isitefhift + 1))) & siteMask) << isitefhift)
                + (L & siteMask)
                );
}

//-------------------------------------
//Get Plaquette
//-------------------------------------
//Forward
//     c
//   +-<-+
// d |   | b ^
//   +->-+
//     a
// b_dir = bounddir, p_dir = plaquette dir
// a = [site][b_dir]
// b = [site+b_dir][p_dir]
// c = [site+p_dir][b_dir]^-1
// d = [site][p_dir]^-1
// Uabcd
// return uint3(b, c, d)
inline uint3 GetForwardPlaquette(uint4 fs, uint plaquttedir, uint bounddir, uint L, uint isitefhift, uint _mask2, RWTexture2D<int2> conf, uint iM, uint iN)
{
    uint Lprime1 = (fs[plaquttedir] + L);
    uint Lprime2 = (fs[bounddir] + L);

    uint gb1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(Lprime2, isitefhift, _mask2), plaquttedir);
    uint gc1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(Lprime1, isitefhift, _mask2), bounddir);
    uint gd1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(L, isitefhift, _mask2), plaquttedir);
    return uint3(gb1, iM + iN - 2 - gc1, iM + iN - 2 - gd1);
}

//Backward
//     a
//   +-<-+
// d |   | b ^
//   +->-+
//     c
// b_dir = bounddir, p_dir = plaquette dir
// a = [site][b_dir]^-1
// b = [site+b_dir-p_dir][p_dir]
// c = [site-p_dir][b_dir]
// d = [site-p_dir][p_dir]^-1
// Ucbad
// return uint3(c, b, d)
inline uint3 GetBackwardPlaquette(uint4 fs, uint4 bs, uint plaquttedir, uint bounddir, uint L, uint isitefhift, uint _mask2, RWTexture2D<int2> conf, uint iM, uint iN)
{
    uint Lprime3 = (bs[plaquttedir] + L);
    uint Lprime4 = (bs[plaquttedir] + fs[bounddir] + L);
    uint gb2 = GetGvalueG4096_4D(Configuration, GetSiteIndexG4096_4D(Lprime4, isitefhift, _mask2), plaquttedir);
    uint gc2 = GetGvalueG4096_4D(Configuration, GetSiteIndexG4096_4D(Lprime3, isitefhift, _mask2), bounddir);
    uint gd2 = GetGvalueG4096_4D(Configuration, GetSiteIndexG4096_4D(Lprime3, isitefhift, _mask2), plaquttedir);
    return uint3(gc2, gb2, iM + iN - 2 - gd2);
}

//-------------------------------------
//Get Plaquette
//-------------------------------------
inline float2 CalcEnergy(uint plaquttedir, uint2 oldA, uint2 newA, uint3 plaq, RWTexture2D<int> mt, RWTexture2D<float> etable)
{
    uint resultGIndexOld = ((plaquttedir & 1) == 0) ?
          mt[uint2(mt[uint2(oldA.x, plaq.x)], mt[uint2(plaq.y, plaq.z)])]
        : mt[uint2(mt[uint2(plaq.x, plaq.y)], mt[uint2(oldA.y, plaq.z)])];

    uint resultGIndexNew = ((plaquttedir & 1) == 0) ?
          mt[uint2(mt[uint2(newA.x, plaq.x)], mt[uint2(plaq.y, plaq.z)])]
        : mt[uint2(mt[uint2(plaq.x, plaq.y)], mt[uint2(newA.y, plaq.z)])];

    return float2(etable[uint2(resultGIndexOld, 0)], etable[uint2(resultGIndexNew, 0)]);
}

inline float CalcEnergySingle(uint plaquttedir, uint oldA, uint3 plaq, RWTexture2D<int> mt, RWTexture2D<float> etable)
{
    uint resultGIndexOld = mt[uint2(mt[uint2(oldA, plaq.x)], mt[uint2(plaq.y, plaq.z)])];
    return etable[uint2(resultGIndexOld, 0)];
}

//-------------------------------------
//Compare and exchange energy
//-------------------------------------
inline uint2 Exchange(uint2 oldA, uint2 newA, float2 energy2, float frandom)
{
    if (energy2.x > energy2.y)
    {
        return newA;
    }

    if (frandom < exp(energy2.x - energy2.y))
    {
        return newA;
    }
    return oldA;
}

//The noise texture is not a good way because it will be affected by muliti-thread problem
//Using this function
//https://stackoverflow.com/questions/4200224/random-noise-functions-for-glsl

#define PHI 0.161803398874989484820459f
#define PI 0.314159265358979323846264f
#define SQ2 14142.1356237309504880169f

inline float gold_noise(uint2 coordinate, float seed) 
{
    return frac(tan(distance(float2(coordinate.x*(seed + PHI), coordinate.y*(seed + PHI)), float2(PHI, PI)))*SQ2);
}


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
//for site shift = 4, siteMask = 1111
//x = L >> (3 * (isitefhift + 1)) & siteMask
//y = L >> (2 * (isitefhift + 1)) & siteMask
//z = L >> (1 * (isitefhift + 1)) & siteMask
//t = L >> (0 * (isitefhift + 1)) & siteMask
//return = (x << site_shift + y, z << site_shit, t)
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
// uint2 = Texture coordinate
// x,y,z = lattice coordinate, same as above
// for z = 1001
// we store it at X = x + (01) << 4, Y = y + (10) << 4.
// X = x + (1001 & 0011) << 4
// Y = y + (1001 & 1100) << 2
//-------------------------------------
inline uint2 GetSiteIndexG4096_3D(uint L, uint isitefhift, uint siteMask, uint halfmaskup, uint halfmaskdown, uint halfshift)
{
    uint x = (L >> (2 * (isitefhift + 1))) & siteMask;
    uint y = (L >> (isitefhift + 1)) & siteMask;
    uint z = L & siteMask;

    //z have upper part in y, and lower part in x
    return uint2(x + (z & halfmaskdown) << isitefhift, y + (z & halfmaskup) << halfshift);
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
inline uint3 GetForwardPlaquette4D(uint4 fs, uint plaquttedir, uint bounddir, uint L, uint isitefhift, uint _mask2, RWTexture2D<int2> conf, uint iM, uint iN)
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
inline uint3 GetBackwardPlaquette4D(uint4 fs, uint4 bs, uint plaquttedir, uint bounddir, uint L, uint isitefhift, uint _mask2, RWTexture2D<int2> conf, uint iM, uint iN)
{
    uint Lprime3 = (bs[plaquttedir] + L);
    uint Lprime4 = (bs[plaquttedir] + fs[bounddir] + L);
    uint gb2 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(Lprime4, isitefhift, _mask2), plaquttedir);
    uint gc2 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(Lprime3, isitefhift, _mask2), bounddir);
    uint gd2 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_4D(Lprime3, isitefhift, _mask2), plaquttedir);
    return uint3(gc2, gb2, iM + iN - 2 - gd2);
}

//==================
//The version for 3D
inline uint3 GetForwardPlaquette3D(uint4 fs, uint plaquttedir, uint bounddir, uint L, uint isitefhift, uint _mask2, RWTexture2D<int2> conf, uint iM, uint iN, uint halfmaskup, uint halfmaskdown, uint halfshift)
{
    uint Lprime1 = (fs[plaquttedir] + L);
    uint Lprime2 = (fs[bounddir] + L);

    uint gb1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_3D(Lprime2, isitefhift, _mask2, halfmaskup, halfmaskdown, halfshift), plaquttedir);
    uint gc1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_3D(Lprime1, isitefhift, _mask2, halfmaskup, halfmaskdown, halfshift), bounddir);
    uint gd1 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_3D(L, isitefhift, _mask2, halfmaskup, halfmaskdown, halfshift), plaquttedir);
    return uint3(gb1, iM + iN - 2 - gc1, iM + iN - 2 - gd1);
}

inline uint3 GetBackwardPlaquette3D(uint4 fs, uint4 bs, uint plaquttedir, uint bounddir, uint L, uint isitefhift, uint _mask2, RWTexture2D<int2> conf, uint iM, uint iN, uint halfmaskup, uint halfmaskdown, uint halfshift)
{
    uint Lprime3 = (bs[plaquttedir] + L);
    uint Lprime4 = (bs[plaquttedir] + fs[bounddir] + L);
    uint gb2 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_3D(Lprime4, isitefhift, _mask2, halfmaskup, halfmaskdown, halfshift), plaquttedir);
    uint gc2 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_3D(Lprime3, isitefhift, _mask2, halfmaskup, halfmaskdown, halfshift), bounddir);
    uint gd2 = GetGvalueG4096_4D(conf, GetSiteIndexG4096_3D(Lprime3, isitefhift, _mask2, halfmaskup, halfmaskdown, halfshift), plaquttedir);
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


//iSiteShift max is 6 (for 64 x 64 x 64 x 64 sites)
//max is 2^(4 x 7) - 1=2^28 - 1
//x = (id.x & mask1) >> iSiteShift
//y = (id.x & mask2)
//z = (id.y & mask1) >> iSiteShift
//w = (id.y & mask2)
//L = x << (iSiteShift + 1) * 3 + y << (iSiteShift + 1) * 2 + z << iSiteShift + 1
#define id_to_Lsite_4d (id.x & mask1) << (2 * iSiteShift + 3) \
                     + (id.x & mask2) << (2 * (iSiteShift + 1)) \
                     + (id.y & mask1) << 1 \
                     + (id.y & mask2)


//x = id.x & mask2
//y = id.y & mask2
//for Z lower part in x, higher part in y, when x = 11 0101, y = 01 1001, z = 0111 = (y & 110000) >> 2 + (x & 110000) >> 4.
//z = id.x & mask4 >> iSiteShift + id.y & mask4 >> iHalfSiteShift
//L = x << (iSiteShift + 1) * 2 + y << (iSiteShift + 1) + z
#define id_to_Lsite_3d (id.x & mask2) << (2 * iSiteShift + 2) \
                     + (id.y & mask2) << (iSiteShift + 1) \
                     + (id.x & mask4) >> iSiteShift + (id.y & mask4) >> iHalfSiteShift


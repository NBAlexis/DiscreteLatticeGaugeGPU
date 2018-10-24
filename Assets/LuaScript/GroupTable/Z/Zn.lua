
-- Zn group, it is Exp(2i n pi / N), let zn = 6 for Z6 group
local zn = 2

-- For n is even, there are two elements, whoes inverse is it self, For n is odd, there is only one.
-- Return m=#(Zn) and n=#(element which inverse is itself)
function GetGroupElementNumber()
    if zn % 2 == 1  then
        return zn, 1
    end
    return zn, 2
end

-- G(1), and G(n/2 +1) is self inverse. G(n)=Exp ( 2i (n-1) pi/N )
-- When n=even
-- G'=( G(n/2 + 1),  G(2), G(3), ... , G(1) , G((m+n)/2 + 1), G((m+n)/2 + 2), ... , G(n/2 + 1)  )
-- For example Z6: G'=( G(4), G(2), G(3), G(1), G(5), G(6), G(4) )
-- When n=odd
-- G'=( G(2), G(3), ... , G(1) , G(n), G(n-1), ... )
-- For example Z7: G'=( G(2), G(3), G(4), G(1), G(5), G(6), G(7) )
-- Check list
-- inverse(G'(i)) = G'(m + n - i)
local function GetGroupIndex(i)
    local m = zn
    local n = 1
    if zn % 2 == 1  then
        -- odd
        if 2*i == (m + n) then
            return 1
        end

        if 2*i < (m + n) then
            return i + 1
        end

        --2*i > (m + n)
        return i
    end    

    -- even
    n = 2
    if 1 == i then
        return 1 + zn / 2
    end
    if zn + 1 == i then
        return 1 + zn / 2
    end
    if 2*i == (m + n) then
        return 1
    end

    -- 2*i > (m + n), 2*i < (m + n), same
    return i
end

-- if G'(i) G'(j) = G'(k), return k
-- Note when there are two k (self-inverse), return the one with k <= Zn
function GetMTTable(i, j)
    local index1 = GetGroupIndex(i)
    local index2 = GetGroupIndex(j)

    -- The element = Exp( 2i (index1 + index2 - 2) pi / N).
    local index3 = ((index1 + index2 - 2) % zn) + 1

    if zn % 2 == 1  then
        -- Odd
        if 1 == index3 then
            return (zn + 1) / 2
        end
        if 2 * index3 <= (zn + 1) then
            return index3 - 1
        end
        return index3
    end

    -- Even
    if 1 == index3 then
        return zn / 2 + 1
    end
    if 2*(index3 - 1) == zn then
        return 1
    end
    return index3
end

-- 1 - (1/2N)*Re(Tr(G'(i)+G'(zn + i - 1)))
function GetETTable(i)
    local index1 = GetGroupIndex(i)
    local index2 = 1
    if zn % 2 == 1  then
        index2 = GetGroupIndex(zn + 1 - i)
    else
        index2 = GetGroupIndex(zn + 2 - i)
    end
    
    -- The element = Exp(2 i (index - 1) pi / zn)
    return 1.0 - 0.5 * (math.cos(2*(index1 - 1)*math.pi / zn)+ math.cos(2*(index2 - 1)*math.pi / zn))
end

-- The smallest non-vanish
-- rotate forward, or rotate backward, G(2) or G(zn)
function GetIGTableNumber()
    -- Z2 is special
    if 2 == zn then
        return 1
    end

    return 2
end

-- rotate forward, or rotate backward, G(2) or G(zn)
function GetIGTable(i)
    -- Z2 is special
    if 2 == zn then
        return 1
    end

    if zn % 2 == 1  then
        -- Odd
        if 1 == i then
            -- G'(1) = G(2)
            return 1
        end
        -- G'(zn) = G(zn)
        return zn
    end

    -- Even
    if 1 == i then
        -- G'(2) = G(2)
        return 2
    end
    -- G'(zn) = G(zn)
    return zn
end

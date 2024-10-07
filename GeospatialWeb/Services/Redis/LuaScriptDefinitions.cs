using StackExchange.Redis;

namespace GeospatialWeb.Services.Redis;

// https://stackexchange.github.io/StackExchange.Redis/Scripting.html
public static class LuaScriptDefinitions
{
    public static LuaScript PreparedRayCastingLuaScript { get; } = LuaScript.Prepare(RayCastingAlgorithm);

    // Redis does not support filtering by a point within a polygon
    // I would not recommend using this script in production
    // Cosmos, Mongo, and Postgres offer better performance with polygon types
    public const string RayCastingAlgorithm =
        """
        -- Function to check if a point is inside a polygon using the Ray-casting algorithm
        local function is_point_in_polygon(point, polygon)
            local px, py = point[1], point[2]
            local inside = false
            local j = #polygon

            for i = 1, #polygon do
                local xi, yi = polygon[i][1], polygon[i][2]
                local xj, yj = polygon[j][1], polygon[j][2]

                if ((yi > py) ~= (yj > py)) and (px < (xj - xi) * (py - yi) / (yj - yi) + xi) then
                    inside = not inside
                end
                j = i
            end

            return inside
        end

        -- Retrieve keys with the given prefix using SCAN
        local prefix = @keyPrefix -- Instead of: KEYS[1]
        local cursor = '0'
        local point = {tonumber(@pointLng), tonumber(@pointLat)} -- Instead of: {tonumber(ARGV[1]), tonumber(ARGV[2])}

        repeat
            local result = redis.call('SCAN', cursor, 'MATCH', prefix .. ':*')
            cursor = result[1]
            local keys = result[2]

            -- Iterate over each key and check if the point is inside any polygon
            for _, key in ipairs(keys) do
                local polygon = {}
                local hash = redis.call('HGETALL', key)
                for i = 1, #hash, 2 do
                    local coords = {}
                    local data = hash[i + 1]
                    coords[1] = struct.unpack('d', data:sub(1, 8))
                    coords[2] = struct.unpack('d', data:sub(9, 16))
                    table.insert(polygon, coords)
                end

                if is_point_in_polygon(point, polygon) then
                    return key  -- Return the key of the polygon that contains the point
                end
            end
        until cursor == '0'

        return nil  -- Return nil if no polygon contains the point
        """;
}

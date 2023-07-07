#region Usings
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Runtime.InteropServices;
#endregion

//Clases y variables usadas en ejercicios en region de abajo
#region Clases Utilizadas
public class Pirate
{
    public string name;
    public CrewPosition myPosition;
    public Prosthesis myProst;
    public int defense;
    public float percentLife;
    public float loyalty;
}

public class Ship
{
    public IEnumerable<Map> myMaps;
    public IEnumerable<Pirate> crew;
    public float baseRationsPerPirate = 12;
    public int questCompleted;
    public int maxDmg;
    public float intimidation;
    public int maxCapacity;
}
public class ActionToDo
{
    public string name = "";
}
public class Map
{
    public bool isOnDangerZone;
    public int gold;
    public int distance;
    public IEnumerable<ActionToDo> steps;
    public IEnumerable<Warnings> stepWarn;
}

public enum Prosthesis { None = 0, Hook = 1, WoodenLeg = 3, EyePatch = 7, GlassEye = 9 }
public enum CrewPosition { Captain = 1, Commander = 2, Sailor = 3, ShipBoy = 4 }
public enum Warnings { Safe, Dangerous, Jewels, Gold, MagicObject }

#endregion

public class Ejercicios
{
    /*En la siguiente funcion se quiere obtener solo los tripulantes de
     * los barcos(parametro pasado "ships") que cumplan con las condiciones dadas(parametro pasado "conditionsCheck"),
     * en una sola coleccion final ordenada de mayor a menor segun su porcentaje de vida de cada tripulante (variable de Pirate "percentLife").
    */
    public List<Pirate> GetPirates(IEnumerable<Ship> ships, Func<Ship, bool> conditionsCheck)
    {
        var result = ships.Where(x => conditionsCheck(x)).SelectMany(x => x.crew).OrderByDescending(x => x.percentLife).ToList();

        return result;
    }

    /*En la siguiente funcion se quiere obtener una coleccion que contenga a todos los piratas de 
     * un barco(parametro pasado "ship"), que no sean capitanes (variable de Pirate "myPosition")
     * y tampoco tengan garfios (Hook)(variable de Pirate "myProst"), junto con su respectivo name. 
     * Ademas deberán estar ordenados por nombre.
     * 
     * EJ de resultado:
     * {(Pirata1,"Brook"),(Pirata2,"Chopper"),(Pirata3,"Sanji"),...}
    */
    public IEnumerable<Tuple<Pirate, string>> GetCrewWithHook(Ship ship)
    {
        var result = ship.crew.Where(x => x.myPosition != CrewPosition.Captain).Where(x => x.myProst != Prosthesis.Hook)
                                          .Select(x => Tuple.Create(x, x.name)).OrderBy(x => x.Item2);

        return result;
    }

    /*Devolver la cantidad de comida a consumir por dia de todos los piratas de puesto CrewPosition.ShipBoy y CrewPosition.Sailor 
     * sabiendo que, el valor de la comida de cada pirata por dia es “baseRationsPerPirate”(Variable en Ship) dividido su puesto(variable de Pirate "myPosition"). 
     * Utilice por lo menos un Aggregate. (además, puede usar otras funciones de LINQ para complementar, esto no quiere decir que sea necesario)
     * Castee el enum(CrewPosition) a int para obtener el valor de CrewPosition.
     * Ej de casteo: (int)pirate.myPosition
    */
    public float GetShipLowCrewFoodRationPerDay(Ship ship)
    {
        var result = ship.crew.Where(x => x.myPosition == CrewPosition.ShipBoy || x.myPosition == CrewPosition.Sailor).Aggregate(0, (x, y) =>
        {
            int foodPerDay = (int)ship.baseRationsPerPirate / (int)y.myPosition;

            return x + foodPerDay;
        });

        return (float)result;
    }

    /*A partir de un barcoPropio(Parametro "ownShip") y de una colección de barcos enemigos(Parametro "enemyShips")
     * se desea obtener una nueva colección con informacion de los barcos enemigos. 
     * La nueva coleccion debera contener en cada elemento:
        -El barco enemigo
        -El oro que contiene. Se calcula multiplicando la cantidad de misiones * 500 de oro * el número de prótesis poseídas.(mientras no sea Prosthesis.None significa que tine protesis)
        -Supervivientes en BarcoPropio estimados en caso de ser asaltado. Se tiene un daño total que el barcoEnemigo puede realizar(variable "maxDmg" en Ship)
            y cada tripulante tiene una defensa(variable de Pirate "defense") que es cuánto daño puede resistir, si es menor al maxDmg del barco enemigo, no sobrevivira.
        -Posibilidades de victoria contra este: se calcula dividiendo el daño del barco enemigo contra mi daño.
            
    Usar Aggregate (además, puede usar otras funciones de LINQ para complementar)
     */
    //IEnumerable<Tuple<BarcoEnemigo,oroEnBarcoEnemigo,SupervivientesProipios,PosibilidadDeVictoria>>
   
    public IEnumerable<Tuple<Ship, int, int, float>> GetBattlesData2(Ship ownShip, IEnumerable<Ship> enemyShips)
    {
        var result = enemyShips.Select(x =>
        {
            int prothesis = x.crew.Select(x => (int)x.myProst).Aggregate(0, (x, y) => x + y);

            int gold = x.questCompleted * 500 * prothesis;

            int enemyDamage = x.maxDmg;           

            //si la defensa de mi tripulante es menor al daño del barcoEnemigo lo saco de la lista (muere)

            int survivors = ownShip.crew.Aggregate(0, (x, y) =>
            {
                if (y.defense < enemyDamage)
                    ownShip.crew.ToList().Remove(y);

                return x + ownShip.crew.Count();
            });

            float victoryChances = enemyDamage / x.maxDmg;

            return Tuple.Create(x, gold, survivors, victoryChances);
        });

        return result;
    }
    

    /*Hay rumores de motin....
     * Se desea obtener una colección de piratas(de la tripulacion de "ship") para una nueva tripulacion de mas confianza
     * Se unirán hasta que uno posea una lealtad(variable "loyalty" en Pirate) mayor a la influencia(variable "intimidation" en Ship) de nuestro barco
     * mientras que haya espacio en el barco (variable "maxCapacity" en Ship)
     * En caso de que pase cualquiera de las dos, el resto dormirá con los peces.
     * Devolver La nueva tripulacion
     */
    public IEnumerable<Pirate> GetNewCrew(Ship ship)
    {
        var result = ship.crew.TakeWhile(x => x.loyalty < ship.intimidation || ship.crew.Count() < ship.maxCapacity);

        return result;
    }

    /*Se quiere obtener el mejor Mapa del tesoro disponible.
     * Primero, no tiene que estar en una zona peligrosa(variable "isOnDangerZone" en Map).
     *Luego, la mejor Opcion se priorizaria por mayor oro(variable "gold" en Map) y de haber mas de 1, el mas cercano(variable "distance" en Map).  

      Asumir que siempre va a haber un mapa que cumpla con nuestras condiciones
         */
    public Map GetBetTreasureMap(IEnumerable<Map> maps)
    {
        var result = maps.Where(x => !x.isOnDangerZone).OrderByDescending(x => x.gold).ThenBy(x => x.distance).FirstOrDefault();
        return result;
    }

    /* Devolver una colección que contenga cada uno de los pasos a seguir(variable "steps" en Map)
     * y su advertencia respectiva(variable "stepWarn" en Map)
     * (Ambas collecciones estan ordenadas de igual manera, por lo que una accion tiene su warning en el mimso indice en la colleccion de advertencias)
     * Se deberán evitar aquellos pasos peligrosos.(no incluir accion si es peligrosa(stepWarn = Warning.Dangerous))
     */
    public IEnumerable<Tuple<ActionToDo, Warnings>> GetActionsToTreasureMap(Map map)
    {
        var result = map.steps.Zip(map.stepWarn, (x, y) => Tuple.Create(x, y)).Where(x => x.Item2 != Warnings.Dangerous);

        return result;
    }
}
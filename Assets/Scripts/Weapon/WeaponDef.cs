using UnityEngine;

namespace Lake.Weapon
{
    public enum WeaponId : byte
    {
        None = 0,
        Rifle = 1,
        Shotgun = 2,
    }

    /// <summary>
    /// Тюнинг одного оружия. Это единственное место, где живут числа ствола:
    /// баллистика, отдача, разброс, оверлей прицеливания и якоря видового крюка.
    ///
    /// Баллистика честная по ощущениям, а не по ЛОР-таблицам: v0 и drag подобраны
    /// так, чтобы на реальных дистанциях леса (10–250 м) пуля читаемо провисала,
    /// но не превращала стрельбу в расчёт обходного листа.
    /// </summary>
    public sealed class WeaponDef
    {
        public static readonly WeaponDef Rifle = new WeaponDef(
            WeaponId.Rifle,
            "ВИНТОВКА",
            muzzleSpeed: 830f,
            drag: 6.0e-5f,
            simStep: 1f / 240f,
            pellets: 1,
            fireInterval: 0.42f,
            magazine: 5,
            reloadSeconds: 2.6f,
            spreadHipDeg: 1.5f,
            spreadAdsDeg: 0.10f,
            spreadMoveDeg: 1.1f,
            spreadAirDeg: 2.4f,
            crouchSpreadFactor: 0.7f,
            bloomPerShotDeg: 0.30f,
            bloomMaxDeg: 1.2f,
            bloomRecovery: 5f,
            recoilPitchDeg: 1.5f,
            recoilYawDeg: 0.5f,
            kickBackM: 0.04f,
            viewKickDeg: 2.6f,
            adsZoom: 2.6f,
            adsMouse: 0.5f,
            adsSpeed: 8f,
            adsOverlay: true,
            hipAnchor: new Vector3(0.15f, -0.135f, 0.34f),
            // Без подушки: тело ниже линии глаза (труба прицела мелькает под
            // перекрестием только в середине подъёма), ближняя грань на
            // 0,30 − 0,16 = 0,14 м от глаза. На полной оптике модель скрывается.
            adsAnchor: new Vector3(0.0f, -0.09f, 0.30f),
            muzzleLocal: new Vector3(0f, 0.005f, 0.52f),
            gunYawDeg: 0f,
            aimPivot: new Vector3(0.15f, -0.11f, -0.28f),
            tracerLength: 20f,
            muzzleLightCandela: 350f,
            muzzleLightRange: 26f,
            muzzleFlashLife: 0.06f,
            impactLightCandela: 50f,
            tracerLightCandela: 9f);

        public static readonly WeaponDef Shotgun = new WeaponDef(
            WeaponId.Shotgun,
            "ДРОБОВИК",
            muzzleSpeed: 390f,
            drag: 3.2e-4f,
            simStep: 1f / 120f,
            pellets: 8,
            fireInterval: 0.95f,
            magazine: 6,
            reloadSeconds: 3.0f,
            spreadHipDeg: 2.6f,
            spreadAdsDeg: 1.4f,
            spreadMoveDeg: 0.8f,
            spreadAirDeg: 2.2f,
            crouchSpreadFactor: 0.8f,
            bloomPerShotDeg: 0f,
            bloomMaxDeg: 0f,
            bloomRecovery: 5f,
            recoilPitchDeg: 3.4f,
            recoilYawDeg: 1.4f,
            kickBackM: 0.07f,
            viewKickDeg: 4.5f,
            adsZoom: 1.25f,
            adsMouse: 0.8f,
            adsSpeed: 9f,
            adsOverlay: false,
            hipAnchor: new Vector3(0.155f, -0.145f, 0.36f),
            // Приёмник ниже глаза и дальше от камеры: верхняя грань тела оружия
            // уходит под линию прицеливания (мушка встаёт сразу под перекрестием),
            // ближняя грань остаётся дальше плоскости отсечения (0,34 − 0,17 = 0,17 м),
            // иначе камера заглядывает ВНУТРЬ коробки и оружие «разваливается».
            adsAnchor: new Vector3(0.012f, -0.095f, 0.34f),
            muzzleLocal: new Vector3(0f, 0.012f, 0.70f),
            gunYawDeg: 0f,
            aimPivot: new Vector3(0.15f, -0.11f, -0.28f),
            tracerLength: 9f,
            muzzleLightCandela: 800f,
            muzzleLightRange: 24f,
            muzzleFlashLife: 0.075f,
            impactLightCandela: 90f,
            tracerLightCandela: 12f);

        /// <summary>Сколько снарядов в одном выстреле максимум — жёсткий потолок на сообщение.</summary>
        public const int MaxPellets = 8;

        public readonly WeaponId id;
        public readonly string displayName;
        public readonly float muzzleSpeed;
        public readonly float drag;
        public readonly float simStep;
        public readonly int pellets;
        public readonly float fireInterval;
        public readonly int magazine;
        public readonly float reloadSeconds;
        public readonly float spreadHipDeg;
        public readonly float spreadAdsDeg;
        public readonly float spreadMoveDeg;
        public readonly float spreadAirDeg;
        public readonly float crouchSpreadFactor;
        public readonly float bloomPerShotDeg;
        public readonly float bloomMaxDeg;
        public readonly float bloomRecovery;
        public readonly float recoilPitchDeg;
        public readonly float recoilYawDeg;
        public readonly float kickBackM;
        public readonly float viewKickDeg;
        public readonly float adsZoom;
        public readonly float adsMouse;
        public readonly float adsSpeed;
        public readonly bool adsOverlay;
        public readonly Vector3 hipAnchor;
        public readonly Vector3 adsAnchor;
        public readonly Vector3 muzzleLocal;

        /// <summary>
        /// Доворот модели вокруг вертикали от прицельной оси. Ствол и пули идут
        /// по фактическому повороту модели: ноль — единственное значение, при
        /// котором прицентрированное оружие смотрит ровно в прицел, а пуля
        /// ложится в точку прицела. Оставлено для мелких поправок позы.
        /// </summary>
        public readonly float gunYawDeg;

        /// <summary>
        /// Точка «плеча» в пространстве камеры: вокруг неё оружие разворачивается
        /// на прицел. Поворот на прицел уводит весь корпус оружия за собой (дуло
        /// идёт по дуге, рукоять тащится), а не крутит ствол на месте, как турель.
        /// </summary>
        public readonly Vector3 aimPivot;

        public readonly float tracerLength;
        public readonly float muzzleLightCandela;
        public readonly float muzzleLightRange;
        public readonly float muzzleFlashLife;
        public readonly float impactLightCandela;
        public readonly float tracerLightCandela;

        /// <summary>Дальность авторитетного пролёта: дальше этой точки пуля «улетает» и рейкасты не тратятся.</summary>
        public readonly float maxRange;

        /// <summary>Потолок времени полёта: страхует от вечных снарядов на выточенных клиентом векторах.</summary>
        public readonly float maxFlight;

        public WeaponDef(WeaponId id, string displayName,
            float muzzleSpeed, float drag, float simStep, int pellets,
            float fireInterval, int magazine, float reloadSeconds,
            float spreadHipDeg, float spreadAdsDeg, float spreadMoveDeg, float spreadAirDeg, float crouchSpreadFactor,
            float bloomPerShotDeg, float bloomMaxDeg, float bloomRecovery,
            float recoilPitchDeg, float recoilYawDeg, float kickBackM, float viewKickDeg,
            float adsZoom, float adsMouse, float adsSpeed, bool adsOverlay,
            Vector3 hipAnchor, Vector3 adsAnchor, Vector3 muzzleLocal, float gunYawDeg,
            Vector3 aimPivot,
            float tracerLength, float muzzleLightCandela, float muzzleLightRange, float muzzleFlashLife,
            float impactLightCandela, float tracerLightCandela)
        {
            this.id = id;
            this.displayName = displayName;
            this.muzzleSpeed = muzzleSpeed;
            this.drag = drag;
            this.simStep = simStep;
            this.pellets = Mathf.Clamp(pellets, 1, MaxPellets);
            this.fireInterval = Mathf.Max(0.08f, fireInterval);
            this.magazine = Mathf.Max(1, magazine);
            this.reloadSeconds = Mathf.Max(0.3f, reloadSeconds);
            this.spreadHipDeg = Mathf.Max(0.02f, spreadHipDeg);
            this.spreadAdsDeg = Mathf.Max(0f, spreadAdsDeg);
            this.spreadMoveDeg = spreadMoveDeg;
            this.spreadAirDeg = spreadAirDeg;
            this.crouchSpreadFactor = Mathf.Clamp(crouchSpreadFactor, 0.4f, 1f);
            this.bloomPerShotDeg = Mathf.Max(0f, bloomPerShotDeg);
            this.bloomMaxDeg = bloomMaxDeg;
            this.bloomRecovery = Mathf.Max(0.5f, bloomRecovery);
            this.recoilPitchDeg = recoilPitchDeg;
            this.recoilYawDeg = recoilYawDeg;
            this.kickBackM = kickBackM;
            this.viewKickDeg = viewKickDeg;
            this.adsZoom = Mathf.Max(1f, adsZoom);
            this.adsMouse = Mathf.Clamp(adsMouse, 0.2f, 1.2f);
            this.adsSpeed = Mathf.Max(2f, adsSpeed);
            this.adsOverlay = adsOverlay;
            this.hipAnchor = hipAnchor;
            this.adsAnchor = adsAnchor;
            this.muzzleLocal = muzzleLocal;
            this.gunYawDeg = gunYawDeg;
            this.aimPivot = aimPivot;
            this.tracerLength = tracerLength;
            this.muzzleLightCandela = muzzleLightCandela;
            this.muzzleLightRange = muzzleLightRange;
            this.muzzleFlashLife = muzzleFlashLife;
            this.impactLightCandela = impactLightCandela;
            this.tracerLightCandela = tracerLightCandela;

            // 700 м винтовки / 150 м дроби — потолок осмысленного попадания и потолок
            // рейкастов: за пределами всё равно промах в туман.
            maxRange = id == WeaponId.Shotgun ? 150f : 700f;
            maxFlight = id == WeaponId.Shotgun ? 0.6f : 1.4f;
        }

        public static WeaponDef Get(byte id)
        {
            switch ((WeaponId)id)
            {
                case WeaponId.Rifle: return Rifle;
                case WeaponId.Shotgun: return Shotgun;
                default: return null;
            }
        }
    }
}

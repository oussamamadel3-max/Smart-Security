# Staging — حدود شبكة (قالب OPS-T-02)

**الحالة:** قالب توثيقي — لا مضيف staging حي في هذه المهمة.

## أهداف

- PostgreSQL غير معرّض للعامة (شبكة خاصة / `127.0.0.1` أو VPC فقط).
- MQTT يستمع داخل الشبكة الخاصة؛ TLS إلزامي عند التفعيل لاحقاً.
- API خلف reverse proxy على 443 فقط للعامة.
- لا فتح منافذ إدارة DB/MQTT للعامة.

## قواعد مقترحة

| مصدر | مقصد | منفذ | قرار |
|------|------|------|------|
| Internet | Reverse proxy | 443/tcp | Allow |
| Internet | PostgreSQL | 5432 | **Deny** |
| Internet | MQTT 1883/8883 | — | **Deny** (إلا VPN/bastion إن لزم لاحقاً بموافقة) |
| App subnet | PostgreSQL | 5432 | Allow |
| App subnet | MQTT | 8883 | Allow (TLS) عند التفعيل |
| Dev laptop | Staging MQTT/DB | — | **Deny** افتراضياً — يمنع أمر جهاز staging من محطة تطوير بلا قناة معتمدة |

## منع العبور بين البيئات

- لا peering بين شبكة staging وشبكة production دون موافقة منفصلة.
- هويات MQTT staging لا تُعاد على وسيط production.
- Connection strings وJWT وFirebase منفصلة بالكامل.
